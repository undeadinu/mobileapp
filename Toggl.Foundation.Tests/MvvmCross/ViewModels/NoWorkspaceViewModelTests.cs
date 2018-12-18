using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class NoWorkspaceViewModelTests
    {
        public abstract class NoWorkspaceViewModelTest : BaseViewModelTests<NoWorkspaceViewModel>
        {
            protected override NoWorkspaceViewModel CreateViewModel()
                => new NoWorkspaceViewModel(DataSource, InteractorFactory, NavigationService, AccessRestrictionStorage, SchedulerProvider, RxActionFactory);
        }

        public sealed class TheConstructor : NoWorkspaceViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useAccessRestrictionStorage,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useSchedulerProvider,
                bool useRxActionFactory)
            {
                var dataSource = useDataSource ? DataSource : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new NoWorkspaceViewModel(dataSource, interactorFactory, navigationService, accessRestrictionStorage, schedulerProvider, rxActionFactory);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheTryAgainCommand : NoWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesWhenAnotherWorkspaceIsFetched()
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(new List<IThreadSafeWorkspace>() { workspace }));

                ViewModel.TryAgain.Execute();
                TestScheduler.Start();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Unit.Default);
            }

            [Fact, LogIfTooSlow]

            public async Task ResetsNoWorkspaceStateWhenAnotherWorkspaceIsFetched()
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(new List<IThreadSafeWorkspace>() { workspace }));

                ViewModel.TryAgain.Execute();
                TestScheduler.Start();

                AccessRestrictionStorage.Received().SetNoWorkspaceStateReached(Arg.Is(false));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNothingWhenNoWorkspacesAreFetched()
            {
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new List<IThreadSafeWorkspace>()));

                ViewModel.TryAgain.Execute();
                TestScheduler.Start();

                await NavigationService.DidNotReceive().Close(Arg.Is(ViewModel));
                AccessRestrictionStorage.DidNotReceive().SetNoWorkspaceStateReached(Arg.Any<bool>());
            }

            [Fact, LogIfTooSlow]
            public async Task StartsAndStopsLoading()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoading.Subscribe(observer);

                var workspace = Substitute.For<IThreadSafeWorkspace>();
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new List<IThreadSafeWorkspace>() { workspace }));

                ViewModel.CreateWorkspaceWithDefaultName.Execute();
                TestScheduler.Start();

                observer.Messages.Count.Should().Be(2);
                observer.Messages[0].Value.Value.Should().BeTrue();
                observer.Messages[1].Value.Value.Should().BeFalse();
            }
        }

        public sealed class TheCreateWorkspaceCommand : NoWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CreatesNewWorkspaceWithDefaultName()
            {
                var name = "Rick Sanchez";
                var user = Substitute.For<IThreadSafeUser>();
                user.Fullname.Returns(name);
                DataSource.User.Current.Returns(Observable.Return(user));

                ViewModel.CreateWorkspaceWithDefaultName.Execute();
                TestScheduler.Start();

                await InteractorFactory.CreateDefaultWorkspace().Received().Execute();
                //workspacesDataSource.Received().Create(Arg.Is($"{name}'s Workspace"));
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesAfterNewWorkspaceIsCreated()
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                InteractorFactory.CreateDefaultWorkspace().Execute().Returns(Observable.Return(Unit.Default));
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new List<IThreadSafeWorkspace> { workspace }));

                ViewModel.CreateWorkspaceWithDefaultName.Execute();
                TestScheduler.Start();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Unit.Default);
            }

            [Fact, LogIfTooSlow]
            public async Task ResetsNoWorkspaceStateWhenAfterNewWorkspaceIsCreated()
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                InteractorFactory.CreateDefaultWorkspace().Execute().Returns(Observable.Return(Unit.Default));
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new List<IThreadSafeWorkspace> { workspace }));

                ViewModel.CreateWorkspaceWithDefaultName.Execute();
                TestScheduler.Start();

                AccessRestrictionStorage.Received().SetNoWorkspaceStateReached(Arg.Is(false));
            }

            [Fact, LogIfTooSlow]
            public async Task StartsAndStopsLoading()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoading.Subscribe(observer);
                InteractorFactory.CreateDefaultWorkspace().Execute().Returns(Observable.Return(Unit.Default));

                ViewModel.CreateWorkspaceWithDefaultName.Execute();
                TestScheduler.Start();

                observer.Messages.Count.Should().Be(2);
                observer.Messages[0].Value.Value.Should().BeTrue();
                observer.Messages[1].Value.Value.Should().BeFalse();
            }
        }
    }
}
