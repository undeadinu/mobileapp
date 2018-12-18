using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Reports;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Xunit;
using Microsoft.Reactive.Testing;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class ReportsViewModelTests
    {
        public abstract class ReportsViewModelTest : BaseViewModelTests<ReportsViewModel>
        {
            protected const long WorkspaceId = 10;

            protected IReportsProvider ReportsProvider { get; } = Substitute.For<IReportsProvider>();

            public ReportsViewModelTest()
            {
                var workspaceObservable = Observable.Return(new MockWorkspace { Id = WorkspaceId });
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(workspaceObservable);
            }

            protected override ReportsViewModel CreateViewModel()
            {
                DataSource.ReportsProvider.Returns(ReportsProvider);
                return new ReportsViewModel(DataSource,
                                            TimeService,
                                            NavigationService,
                                            InteractorFactory,
                                            AnalyticsService,
                                            DialogService,
                                            IntentDonationService,
                                            SchedulerProvider,
                                            StopwatchProvider,
                                            RxActionFactory);
            }

            protected async Task Initialize()
            {
                using (var block = new AutoResetEvent(false))
                {
                    NavigationService
                        .When(service => service.Navigate(Arg.Any<ReportsCalendarViewModel>()))
                        .Do(async callInfo =>
                        {
                            var calendarViewModel = callInfo.Arg<ReportsCalendarViewModel>();
                            calendarViewModel.Prepare();
                            await calendarViewModel.Initialize();
                            block.Set();
                        });

                    await ViewModel.Initialize();
                    ViewModel.ViewAppeared();

                    block.WaitOne();
                }
            }
        }

        public sealed class TheConstructor : ReportsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource,
                                                        bool useTimeService,
                                                        bool useNavigationService,
                                                        bool useAnalyticsService,
                                                        bool useInteractorFactory,
                                                        bool useDialogService,
                                                        bool useIntentDonationService,
                                                        bool useSchedulerProvider,
                                                        bool useStopwatchProvider,
                                                        bool useRxActionFactory)
            {
                var timeService = useTimeService ? TimeService : null;
                var reportsProvider = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var dialogService = useDialogService ? DialogService : null;
                var intentDonationService = useIntentDonationService ? IntentDonationService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ReportsViewModel(reportsProvider,
                                               timeService,
                                               navigationService,
                                               interactorFactory,
                                               analyticsService,
                                               dialogService,
                                               intentDonationService,
                                               schedulerProvider,
                                               stopwatchProvider,
                                               rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitializeMethod : ReportsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsSegmentsJustOnceWhenChangingDateRange()
            {
                var segments = new ChartSegment[2] {
                    new ChartSegment("Project 1", "Client 1", 50f, 10, 0, "ff0000"),
                    new ChartSegment("Project 2", "Client 2", 50f, 10, 0, "00ff00")
                };
                var projectsNotSyncedCount = 0;

                var currentDate = new DateTimeOffset(2018, 5, 23, 0, 0, 0, TimeSpan.Zero);
                var start = new DateTimeOffset(2018, 5, 1, 0, 0, 0, TimeSpan.Zero);
                var end = new DateTimeOffset(2018, 5, 7, 0, 0, 0, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(currentDate);

                var delayed = Observable
                    .Return(new ProjectSummaryReport(segments, projectsNotSyncedCount))
                    .Delay(TimeSpan.FromMilliseconds(100));

                var instant = Observable
                    .Return(new ProjectSummaryReport(segments, projectsNotSyncedCount));

                ReportsProvider.GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                               .Returns(delayed, instant);

                await Initialize();
                ViewModel.ChangeDateRangeCommand.Execute(
                    ReportsDateRangeParameter.WithDates(start, end));

                await delayed;
                ViewModel.Segments.Count.Should().Be(segments.Length);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksAnEventWhenReportLoadsSuccessfully()
            {
                var startDateRange = new DateTimeOffset(2018, 05, 05, 0, 0, 0, TimeSpan.Zero);
                var endDateRange = startDateRange.AddDays(7);

                var totalDays = (int)(endDateRange - startDateRange).TotalDays;
                var projectsNotSyncedCount = 0;
                var loadingDuration = TimeSpan.FromSeconds(5);
                var now = new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero);

                TimeService.CurrentDateTime.Returns(_ =>
                {
                    now = now + loadingDuration;
                    return now;
                });

                ReportsProvider.GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                        .Returns(Observable.Return(new ProjectSummaryReport(new ChartSegment[0], projectsNotSyncedCount)));

                await Initialize();

                AnalyticsService.Received().ReportsSuccess.Track(ReportsSource.Initial, totalDays, projectsNotSyncedCount, loadingDuration.TotalMilliseconds);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksAnEventWhenReportFailsToLoad()
            {
                var startDateRange = new DateTimeOffset(2018, 05, 05, 0, 0, 0, TimeSpan.Zero);
                var endDateRange = startDateRange.AddDays(7);

                var totalDays = (int)(endDateRange - startDateRange).TotalDays;
                var loadingDuration = TimeSpan.FromSeconds(5);
                var now = new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero);

                TimeService.CurrentDateTime.Returns(_ =>
                {
                    now = now + loadingDuration;
                    return now;
                });

                ReportsProvider.GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                        .Returns(Observable.Throw<ProjectSummaryReport>(new Exception()));

                await Initialize();

                AnalyticsService.Received().ReportsFailure.Track(ReportsSource.Initial, totalDays, loadingDuration.TotalMilliseconds);
            }
        }

        public sealed class TheBillablePercentageMethod : ReportsViewModelTest
        {
            [Property(MaxTest = 1)]
            public void IsSetToNullIfTheTotalTimeOfAReportIsZero(DateTimeOffset now)
            {
                var date = now.Date;
                var projectsNotSyncedCount = 0;
                TimeService.CurrentDateTime.Returns(now);
                var expectedStartDate = date.AddDays(1 - (int)date.DayOfWeek);
                ReportsProvider.GetProjectSummary(
                    WorkspaceId, expectedStartDate, expectedStartDate.AddDays(6))
                        .Returns(Observable.Return(new ProjectSummaryReport(new ChartSegment[0], projectsNotSyncedCount)));

                ViewModel.Initialize().Wait();

                ViewModel.BillablePercentage.Should().BeNull();
            }
        }

        public sealed class TheIsLoadingProperty : ReportsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task IsSetToTrueWhenTheViewIsInitializedBeforeAnyLoadingOfReportsStarts()
            {
                await ViewModel.Initialize();

                ViewModel.IsLoading.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task IsSetToTrueWhenAReportIsLoading()
            {
                var now = DateTimeOffset.Now;
                TimeService.CurrentDateTime.Returns(now);
                ReportsProvider.GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Never<ProjectSummaryReport>());

                await Initialize();

                ViewModel.IsLoading.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task IsSetToFalseWhenLoadingIsCompleted()
            {
                var now = DateTimeOffset.Now;
                var projectsNotSyncedCount = 0;
                TimeService.CurrentDateTime.Returns(now);
                ReportsProvider.GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(new ProjectSummaryReport(new ChartSegment[0], projectsNotSyncedCount)));

                await Initialize();

                ViewModel.IsLoading.Should().BeFalse();
            }
        }

        public sealed class TheIsLoadingObservable : ReportsViewModelTest
        {
            private readonly ITestableObserver<bool> isLoadingObserver;

            public TheIsLoadingObservable()
            {
                isLoadingObserver = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoadingObservable.Subscribe(isLoadingObserver);
            }

            [Fact, LogIfTooSlow]
            public async Task IsSetToTrueWhenTheViewIsInitializedBeforeAnyLoadingOfReportsStarts()
            {
                await ViewModel.Initialize();

                TestScheduler.Start();
                isLoadingObserver.Messages.Last().Value.Value.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task IsSetToTrueWhenAReportIsLoading()
            {
                var now = DateTimeOffset.Now;
                TimeService.CurrentDateTime.Returns(now);
                ReportsProvider.GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Never<ProjectSummaryReport>());

                await Initialize();

                TestScheduler.Start();
                isLoadingObserver.Messages.Last().Value.Value.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task IsSetToFalseWhenLoadingIsCompleted()
            {
                var now = DateTimeOffset.Now;
                var projectsNotSyncedCount = 0;
                TimeService.CurrentDateTime.Returns(now);
                ReportsProvider.GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(new ProjectSummaryReport(new ChartSegment[0], projectsNotSyncedCount)));

                await Initialize();

                TestScheduler.Start();
                isLoadingObserver.Messages.Last().Value.Value.Should().BeFalse();
            }
        }

        public sealed class TheCurrentDateRangeStringProperty : ReportsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task IsInitializedToEmptyOrNull()
            {
                await ViewModel.Initialize();

                ViewModel.CurrentDateRangeString.Should().BeNullOrEmpty();
            }

            [Theory, LogIfTooSlow]
            [InlineData(
                2017, 12, 6,
                2017, 12, 4,
                2017, 12, 10
            )]
            [InlineData(
                2018, 2, 28,
                2018, 2, 26,
                2018, 3, 4
            )]
            [InlineData(
                2016, 4, 20,
                2016, 4, 18,
                2016, 4, 24
            )]
            [InlineData(
                2016, 12, 28,
                2016, 12, 26,
                2017, 1, 1
            )]
            public void ReturnsThisWeekWhenStartAndEndOfCurrentWeekAreSeleted(
                int currentYear, int currentMonth, int currentDay,
                int startYear, int startMonth, int startDay,
                int endYear, int endMonth, int endDay)
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.CurrentDateRangeStringObservable.Subscribe(observer);

                var currentDate = new DateTimeOffset(currentYear, currentMonth, currentDay, 0, 0, 0, TimeSpan.Zero);
                var start = new DateTimeOffset(startYear, startMonth, startDay, 0, 0, 0, TimeSpan.Zero);
                var end = new DateTimeOffset(endYear, endMonth, endDay, 0, 0, 0, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(currentDate);
                ViewModel.ChangeDateRangeCommand.Execute(
                    ReportsDateRangeParameter.WithDates(start, end).WithSource(ReportsSource.Calendar));

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, ""),
                    ReactiveTest.OnNext(2, $"{Resources.ThisWeek} ▾")
                );
            }

            [Theory]
            [MemberData(nameof(DateRangeFormattingTestData))]
            public async Task ReturnsSelectedDateRangeAsStringIfTheSelectedPeriodIsNotTheCurrentWeek(
                DateTimeOffset start,
                DateTimeOffset end,
                DateFormat dateFormat,
                string expectedResult)
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.CurrentDateRangeStringObservable.Subscribe(observer);

                TimeService.CurrentDateTime.Returns(DateTimeOffset.UtcNow);
                var preferences = Substitute.For<IThreadSafePreferences>();
                preferences.DateFormat.Returns(dateFormat);
                var preferencesSubject = new Subject<IThreadSafePreferences>();
                DataSource.Preferences.Current.Returns(preferencesSubject.AsObservable());
                await ViewModel.Initialize();
                preferencesSubject.OnNext(preferences);

                ViewModel.ChangeDateRangeCommand.Execute(
                    ReportsDateRangeParameter.WithDates(start, end).WithSource(ReportsSource.Calendar));

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, ""),
                    ReactiveTest.OnNext(2, expectedResult)
                );
            }

            public static IEnumerable<object[]> DateRangeFormattingTestData()
            {
                var dateFormats = new[]
                {
                    DateFormat.FromLocalizedDateFormat("YYYY-MM-DD"),
                    DateFormat.FromLocalizedDateFormat("DD.MM.YYYY"),
                    DateFormat.FromLocalizedDateFormat("DD-MM-YYYY"),
                    DateFormat.FromLocalizedDateFormat("MM/DD/YYYY"),
                    DateFormat.FromLocalizedDateFormat("DD/MM/YYYY"),
                    DateFormat.FromLocalizedDateFormat("MM-DD-YYYY")
                };

                var ranges = new[]
                {
                    (new DateTimeOffset(2017, 12, 15, 10, 12, 13, TimeSpan.Zero), new DateTimeOffset(2017, 12, 15, 12, 34, 1, TimeSpan.Zero)),
                    (new DateTimeOffset(2017, 1, 1, 10, 12, 13, TimeSpan.Zero), new DateTimeOffset(2017, 12, 30, 12, 34, 1, TimeSpan.Zero)),
                    (new DateTimeOffset(2017, 11, 13, 10, 12, 13, TimeSpan.Zero), new DateTimeOffset(2018, 11, 13, 12, 34, 1, TimeSpan.Zero))
                };

                string expectedTitleString(DateTimeOffset start, DateTimeOffset end, DateFormat dateFormat)
                    => $"{start.ToString(dateFormat.Short)} - {end.ToString(dateFormat.Short)} ▾";

                foreach (var (start, end) in ranges)
                {
                    foreach (var dateFormat in dateFormats)
                    {
                        yield return new object[]
                        {
                            start,
                            end,
                            dateFormat,
                            expectedTitleString(start, end, dateFormat)
                        };
                    }
                }
            }
        }

        public sealed class TheSegmentsProperty : ReportsViewModelTest
        {
            private readonly int projectsNotSyncedCount = 0;

            [Fact]
            public async Task DoesNotGroupProjectSegmentsWithPercentageGreaterThanOrEqualFivePercent()
            {
                ChartSegment[] segments =
                {
                    new ChartSegment("Project 1", "Client 1", 2, 2, 0, "#ffffff"),
                    new ChartSegment("Project 2", "Client 2", 2, 2, 0, "#ffffff"),
                    new ChartSegment("Project 3", "Client 3", 17, 17, 0, "#ffffff"),
                    new ChartSegment("Project 4", "Client 4", 23, 23, 0, "#ffffff"),
                    new ChartSegment("Project 5", "Client 5", 56, 56, 0, "#ffffff")
                };

                TimeService.CurrentDateTime.Returns(new DateTimeOffset(2018, 05, 15, 12, 00, 00, TimeSpan.Zero));
                ReportsProvider.GetProjectSummary(WorkspaceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(new ProjectSummaryReport(segments, projectsNotSyncedCount)));

                await Initialize();

                ViewModel.Segments.Should().HaveCount(5);
                ViewModel.GroupedSegments.Should().HaveCount(4);
                ViewModel.GroupedSegments.Should().Contain(segment =>
                    segment.ProjectName == Resources.Other &&
                    segment.Percentage == segments[0].Percentage + segments[1].Percentage);
                ViewModel.GroupedSegments
                    .Where(project => project.ProjectName != Resources.Other)
                    .Select(segment => segment.Percentage)
                    .ForEach(percentage => percentage.Should().BeGreaterOrEqualTo(5));
            }

            [Fact]
            public async Task GroupsProjectSegmentsWithPercentageLesserThanOnePercent()
            {
                ChartSegment[] segments =
                {
                    new ChartSegment("Project 1", "Client 1", 0.9f, 2, 0, "#ffffff"),
                    new ChartSegment("Project 2", "Client 2", 0.3f, 3, 0, "#ffffff"),
                    new ChartSegment("Project 3", "Client 3", 7.8f, 4, 0, "#ffffff"),
                    new ChartSegment("Project 4", "Client 4", 12, 12, 0, "#ffffff"),
                    new ChartSegment("Project 5", "Client 5", 23, 23, 0, "#ffffff"),
                    new ChartSegment("Project 6", "Client 6", 56, 56, 0, "#ffffff")
                };

                TimeService.CurrentDateTime.Returns(new DateTimeOffset(2018, 05, 15, 12, 00, 00, TimeSpan.Zero));
                ReportsProvider.GetProjectSummary(WorkspaceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(new ProjectSummaryReport(segments, projectsNotSyncedCount)));

                await Initialize();

                ViewModel.Segments.Should().HaveCount(6);
                ViewModel.GroupedSegments.Should().HaveCount(5);
                ViewModel.GroupedSegments.Should().Contain(segment =>
                    segment.ProjectName == Resources.Other &&
                    segment.Percentage == segments[0].Percentage + segments[1].Percentage);
                ViewModel.GroupedSegments
                    .Where(project => project.ProjectName != Resources.Other)
                    .Select(segment => segment.Percentage)
                    .ForEach(percentage => percentage.Should().BeGreaterOrEqualTo(5));
            }

            [Fact]
            public async Task GroupsOtherProjectsToAtLeastOnePercentRegardlessOfActualPercentage()
            {
                ChartSegment[] segments =
                {
                    new ChartSegment("Project 1", "Client 1", 0.2f, 2, 0, "#ffffff"),
                    new ChartSegment("Project 2", "Client 2", 0.3f, 3, 0, "#ffffff"),
                    new ChartSegment("Project 3", "Client 3", 8.5f, 4, 0, "#ffffff"),
                    new ChartSegment("Project 4", "Client 4", 12, 12, 0, "#ffffff"),
                    new ChartSegment("Project 5", "Client 5", 23, 23, 0, "#ffffff"),
                    new ChartSegment("Project 6", "Client 6", 56, 56, 0, "#ffffff")
                };

                TimeService.CurrentDateTime.Returns(new DateTimeOffset(2018, 05, 15, 12, 00, 00, TimeSpan.Zero));
                ReportsProvider.GetProjectSummary(WorkspaceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(new ProjectSummaryReport(segments, projectsNotSyncedCount)));

                await Initialize();

                ViewModel.Segments.Should().HaveCount(6);
                ViewModel.GroupedSegments.Should().HaveCount(5);
                ViewModel.GroupedSegments.Should().Contain(segment =>
                    segment.ProjectName == Resources.Other &&
                    segment.Percentage == 1f);
                ViewModel.GroupedSegments
                    .Where(project => project.ProjectName != Resources.Other)
                    .Select(segment => segment.Percentage)
                    .ForEach(percentage => percentage.Should().BeGreaterOrEqualTo(5));
            }

            [Fact]
            public async Task GroupsProjectSegmentsWithPercentageBetweenOneAndFiveIntoOtherIfTotalOfOtherLessThanFivePercent()
            {
                ChartSegment[] segments =
                {
                    new ChartSegment("Project 1", "Client 1", 0.9f, 2, 0, "#ffffff"),
                    new ChartSegment("Project 2", "Client 2", 0.9f, 3, 0, "#ffffff"),
                    new ChartSegment("Project 3", "Client 3", 2.5f, 4, 0, "#ffffff"),
                    new ChartSegment("Project 4", "Client 4", 4, 12, 0, "#ffffff"),
                    new ChartSegment("Project 5", "Client 5", 31.7f, 23, 0, "#ffffff"),
                    new ChartSegment("Project 6", "Client 6", 60, 56, 0, "#ffffff")
                };

                TimeService.CurrentDateTime.Returns(new DateTimeOffset(2018, 05, 15, 12, 00, 00, TimeSpan.Zero));
                ReportsProvider.GetProjectSummary(WorkspaceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(new ProjectSummaryReport(segments, projectsNotSyncedCount)));

                await Initialize();

                ViewModel.Segments.Should().HaveCount(6);
                ViewModel.GroupedSegments.Should().HaveCount(4);
                ViewModel.GroupedSegments.Should().Contain(segment =>
                    segment.ProjectName == Resources.Other &&
                    segment.Percentage == segments[0].Percentage + segments[1].Percentage + segments[2].Percentage);
                ViewModel.GroupedSegments
                    .Where(project => project.ProjectName != Resources.Other)
                    .Select(segment => segment.Percentage)
                    .ForEach(percentage => percentage.Should().BeGreaterOrEqualTo(4));
            }

            [Fact]
            public async Task SetsOtherProjectWithOneSegmentToThatSegmentButWithOnePercentIfLessThanOnePercent()
            {
                ChartSegment[] segments =
                {
                    new ChartSegment("Project 1", "Client 1", 0.2f, 2, 0, "#666666"),
                    new ChartSegment("Project 2", "Client 2", 8.8f, 4, 0, "#ffffff"),
                    new ChartSegment("Project 3", "Client 3", 12, 12, 0, "#ffffff"),
                    new ChartSegment("Project 4", "Client 4", 23, 23, 0, "#ffffff"),
                    new ChartSegment("Project 5", "Client 5", 56, 56, 0, "#ffffff")
                };

                TimeService.CurrentDateTime.Returns(new DateTimeOffset(2018, 05, 15, 12, 00, 00, TimeSpan.Zero));
                ReportsProvider.GetProjectSummary(WorkspaceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(new ProjectSummaryReport(segments, projectsNotSyncedCount)));

                await Initialize();

                ViewModel.Segments.Should().HaveCount(5);
                ViewModel.GroupedSegments.Should().HaveCount(5);
                ViewModel.GroupedSegments.Should().Contain(segment =>
                    segment.ProjectName == "Project 1" &&
                    segment.Percentage == 1f &&
                    segment.Color == "#666666");
                ViewModel.GroupedSegments
                    .Where(project => project.ProjectName != "Project 1")
                    .Select(segment => segment.Percentage)
                    .ForEach(percentage => percentage.Should().BeGreaterOrEqualTo(5));
            }
        }

        public sealed class TheSelectWorkspaceCommand : ReportsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ShouldTriggerAReportReload()
            {
                await ViewModel.Initialize();
                var mockWorkspace = new MockWorkspace { Id = WorkspaceId + 1 };
                DialogService.Select(Arg.Any<string>(), Arg.Any<IEnumerable<(string, IThreadSafeWorkspace)>>(), Arg.Any<int>())
                    .Returns(Observable.Return(mockWorkspace));

                ViewModel.SelectWorkspace.Execute();
                TestScheduler.Start();

                await ReportsProvider.Received().GetProjectSummary(Arg.Is(mockWorkspace.Id), Arg.Any<DateTimeOffset>(),
                    Arg.Any<DateTimeOffset>());
            }

            [Fact, LogIfTooSlow]
            public async Task ShouldChangeCurrentWorkspaceName()
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.WorkspaceNameObservable.Subscribe(observer);

                var mockWorkspace = new MockWorkspace { Id = WorkspaceId + 1, Name = "Selected workspace" };
                DialogService.Select(Arg.Any<string>(), Arg.Any<IEnumerable<(string, IThreadSafeWorkspace)>>(), Arg.Any<int>())
                    .Returns(Observable.Return(mockWorkspace));
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(mockWorkspace));

                await ViewModel.Initialize();

                ViewModel.SelectWorkspace.Execute();
                TestScheduler.Start();

                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, ""),
                    ReactiveTest.OnNext(2, mockWorkspace.Name)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task ShouldNotTriggerAReportReloadWhenSelectionIsCancelled()
            {
                await ViewModel.Initialize();
                DialogService.Select(Arg.Any<string>(), Arg.Any<IEnumerable<(string, IThreadSafeWorkspace)>>(), Arg.Any<int>())
                    .Returns(Observable.Return<IThreadSafeWorkspace>(null));

                ViewModel.SelectWorkspace.Execute();
                TestScheduler.Start();

                await ReportsProvider.DidNotReceive().GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(),
                    Arg.Any<DateTimeOffset>());
            }

            [Fact, LogIfTooSlow]
            public async Task ShouldNotTriggerAReportReloadWhenTheSameWorkspaceIsSelected()
            {
                await ViewModel.Initialize();

                var mockWorkspace = new MockWorkspace { Id = WorkspaceId };
                DialogService.Select(Arg.Any<string>(), Arg.Any<IEnumerable<(string, IThreadSafeWorkspace)>>(), Arg.Any<int>())
                    .Returns(Observable.Return<IThreadSafeWorkspace>(mockWorkspace));

                ViewModel.SelectWorkspace.Execute();
                TestScheduler.Start();

                await ReportsProvider.DidNotReceive().GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(),
                    Arg.Any<DateTimeOffset>());
            }
        }

        public sealed class TheStartDateAndEndDateObservables : ReportsViewModelTest
        {
            private readonly ITestableObserver<DateTimeOffset> startDateObserver;
            private readonly ITestableObserver<DateTimeOffset> endDateObserver;

            public TheStartDateAndEndDateObservables()
            {
                startDateObserver = TestScheduler.CreateObserver<DateTimeOffset>();
                endDateObserver = TestScheduler.CreateObserver<DateTimeOffset>();

                ViewModel.StartDate.Subscribe(startDateObserver);
                ViewModel.EndDate.Subscribe(endDateObserver);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotEmitAnyValuesDuringInitialization()
            {
                await ViewModel.Initialize();

                startDateObserver.Messages.Should().BeEmpty();
                endDateObserver.Messages.Should().BeEmpty();
            }

            [Theory, LogIfTooSlow]
            [InlineData(
                2017, 12, 6,
                2017, 12, 4,
                2017, 12, 10
            )]
            [InlineData(
                2018, 2, 28,
                2018, 2, 26,
                2018, 3, 4
            )]
            [InlineData(
                2016, 4, 20,
                2016, 4, 18,
                2016, 4, 24
            )]
            [InlineData(
                2016, 12, 28,
                2016, 12, 26,
                2017, 1, 1
            )]
            public void ReturnsTheCorrectStartAndEndDatesForSelectedRanges(
                int currentYear, int currentMonth, int currentDay,
                int startYear, int startMonth, int startDay,
                int endYear, int endMonth, int endDay)
            {
                var currentDate = new DateTimeOffset(currentYear, currentMonth, currentDay, 0, 0, 0, TimeSpan.Zero);
                var start = new DateTimeOffset(startYear, startMonth, startDay, 0, 0, 0, TimeSpan.Zero);
                var end = new DateTimeOffset(endYear, endMonth, endDay, 0, 0, 0, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(currentDate);
                ViewModel.ChangeDateRangeCommand.Execute(
                    ReportsDateRangeParameter.WithDates(start, end).WithSource(ReportsSource.Calendar));

                TestScheduler.Start();
                startDateObserver.Messages.AssertEqual(ReactiveTest.OnNext(1, start));
                endDateObserver.Messages.AssertEqual(ReactiveTest.OnNext(1, end));
            }
        }

        public sealed class TheWorkspaceHasBillableFeatureEnabledObservable : ReportsViewModelTest
        {
            private readonly ITestableObserver<bool> isEnabledObserver;

            public TheWorkspaceHasBillableFeatureEnabledObservable()
            {
                isEnabledObserver = TestScheduler.CreateObserver<bool>();
                ViewModel.WorkspaceHasBillableFeatureEnabled.Subscribe(isEnabledObserver);
            }

            [Fact]
            public async Task IsDisabledByDefault()
            {
                await ViewModel.Initialize();

                TestScheduler.Start();
                isEnabledObserver.Messages.Single().Value.Value.Should().BeFalse();
            }

            [Fact]
            public async Task StaysDisabledWhenSwitchingToAFreeWorkspace()
            {
                prepareWorkspace(isProEnabled: false);

                await ViewModel.Initialize();

                ViewModel.SelectWorkspace.Execute();
                TestScheduler.Start();

                isEnabledObserver.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact]
            public async Task BecomesEnabledWhenSwitchingToAProWorkspace()
            {
                prepareWorkspace(isProEnabled: true);

                await ViewModel.Initialize();

                ViewModel.SelectWorkspace.Execute();
                TestScheduler.Start();

                isEnabledObserver.Messages.Last().Value.Value.Should().BeTrue();
            }

            private void prepareWorkspace(bool isProEnabled)
            {
                var workspace = new MockWorkspace { Id = 123 };
                var workspaceFeatures = new MockWorkspaceFeatureCollection
                {
                    Features = new[] { new MockWorkspaceFeature { FeatureId = WorkspaceFeatureId.Pro, Enabled = isProEnabled } }
                };

                var workspaceFeaturesObservable = Observable.Return(workspaceFeatures);
                var workspaceObservable = Observable.Return(workspace);
                DataSource.WorkspaceFeatures.GetById(workspace.Id).Returns(workspaceFeaturesObservable);
                DialogService.Select(Arg.Any<string>(), Arg.Any<ICollection<(string, IThreadSafeWorkspace)>>(), Arg.Any<int>()).Returns(workspaceObservable);
            }
        }

        public sealed class TheViewAppearedMethod : ReportsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(3)]
            [InlineData(5)]
            [InlineData(8)]
            [InlineData(13)]
            public async Task ShouldTriggerReloadForEveryAppearance(int numberOfAppearances)
            {
                ReportsProvider.GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(),
                        Arg.Any<DateTimeOffset>())
                    .ReturnsForAnyArgs(Observable.Empty<ProjectSummaryReport>(SchedulerProvider.TestScheduler));
                await ViewModel.Initialize();
                ViewModel.ViewAppeared(); // First call is skipped

                for (int i = 0; i < numberOfAppearances; ++i)
                {
                    ViewModel.ViewAppeared();
                }
                TestScheduler.Start();

                ReportsProvider.Received(numberOfAppearances).GetProjectSummary(Arg.Any<long>(), Arg.Any<DateTimeOffset>(),
                    Arg.Any<DateTimeOffset>());
            }
        }
    }
}
