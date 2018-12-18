using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TimeEntriesViewModel
    {
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        private CompositeDisposable disposeBag = new CompositeDisposable();
        private DurationFormat durationFormat;

        public ObservableGroupedOrderedCollection<TimeEntryViewModel> TimeEntries { get; }
        public IObservable<bool> Empty => TimeEntries.Empty;
        public IObservable<int> Count => TimeEntries.TotalCount;

        private readonly Subject<bool> showUndoSubject = new Subject<bool>();
        private IDisposable delayedDeletionDisposable;
        private TimeEntryViewModel timeEntryToDelete;

        public IObservable<bool> ShouldShowUndo { get; }

        public InputAction<TimeEntryViewModel> DelayDeleteTimeEntry { get; }
        public UIAction CancelDeleteTimeEntry { get; }

        public TimeEntriesViewModel (ITogglDataSource dataSource,
                                     IInteractorFactory interactorFactory,
                                     IAnalyticsService analyticsService,
                                     ISchedulerProvider schedulerProvider,
                                     IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.analyticsService = analyticsService;
            this.schedulerProvider = schedulerProvider;
            this.rxActionFactory = rxActionFactory;

            TimeEntries = new ObservableGroupedOrderedCollection<TimeEntryViewModel>(
                indexKey: t => t.Id,
                orderingKey: t => t.StartTime,
                groupingKey: t => t.StartTime.LocalDateTime.Date,
                descending: true
            );

            DelayDeleteTimeEntry = rxActionFactory.FromAction<TimeEntryViewModel>(delayDeleteTimeEntry);
            CancelDeleteTimeEntry = rxActionFactory.FromAction(cancelDeleteTimeEntry);

            ShouldShowUndo = showUndoSubject.AsObservable().AsDriver(schedulerProvider);
        }

        public async Task Initialize()
        {
            await fetchSectionedTimeEntries();

            disposeBag = new CompositeDisposable();

            dataSource.TimeEntries.Created
                .Where(isNotRunning)
                .Subscribe(onTimeEntryAdded)
                .DisposedBy(disposeBag);

            dataSource.TimeEntries.Deleted
                .Subscribe(onTimeEntryRemoved)
                .DisposedBy(disposeBag);

            dataSource.TimeEntries.Updated
                .Subscribe(onTimeEntryUpdated)
                .DisposedBy(disposeBag);

            dataSource.Preferences.Current
                .Subscribe(onPreferencesChanged)
                .DisposedBy(disposeBag);
        }

        public async Task ReloadData()
        {
            await fetchSectionedTimeEntries();
        }

        private void delayDeleteTimeEntry(TimeEntryViewModel timeEntry)
        {
            timeEntryToDelete = timeEntry;

            onTimeEntryRemoved(timeEntry.Id);
            showUndoSubject.OnNext(true);

            delayedDeletionDisposable = Observable.Merge( // If 5 seconds pass or we try to delete another TE
                    Observable.Return(timeEntry).Delay(Constants.UndoTime, schedulerProvider.DefaultScheduler),
                    showUndoSubject.Where(t => t).SelectValue(timeEntry)
                )
                .Take(1)
                .SelectMany(deleteTimeEntry)
                .Do(te =>
                {
                    if (te == timeEntryToDelete) // Hide bar if there isn't other TE trying to be deleted
                        showUndoSubject.OnNext(false);
                })
                .Subscribe();
        }

        private void cancelDeleteTimeEntry()
        {
            if (!TimeEntries.IndexOf(timeEntryToDelete.Id).HasValue)
            {
                TimeEntries.InsertItem(timeEntryToDelete);
            }

            timeEntryToDelete = null;
            delayedDeletionDisposable.Dispose();
            showUndoSubject.OnNext(false);
        }

        private IObservable<TimeEntryViewModel> deleteTimeEntry(TimeEntryViewModel timeEntry)
        {
            return interactorFactory
                .DeleteTimeEntry(timeEntry.Id)
                .Execute()
                .Do(_ =>
                {
                    analyticsService.DeleteTimeEntry.Track();
                    dataSource.SyncManager.PushSync();
                })
                .SelectValue(timeEntry);
        }

        private async Task fetchSectionedTimeEntries()
        {
            var groupedEntries = await interactorFactory.GetAllTimeEntriesVisibleToTheUser().Execute()
                .Select(entries => entries
                    .Where(isNotRunning)
                    .Where(timeEntry => timeEntry.Id != timeEntryToDelete?.Id)
                    .Select(te => new TimeEntryViewModel(te, durationFormat))
                );

            TimeEntries.ReplaceWith(groupedEntries);
        }

        private void onTimeEntryUpdated(EntityUpdate<IThreadSafeTimeEntry> update)
        {
            var timeEntry = update.Entity;
            if (timeEntry == null) return;

            if (timeEntry.IsDeleted || timeEntry.IsRunning())
            {
                onTimeEntryRemoved(timeEntry.Id);
            }
            else
            {
                var timeEntryViewModel = new TimeEntryViewModel(timeEntry, durationFormat);
                if (timeEntry.Id == timeEntryToDelete?.Id)
                {
                    // Ignore this update because the entity is hidden and might be deleted unless the user
                    // undoes the action. In that case bring the time entry but with the updated data.
                    timeEntryToDelete = timeEntryViewModel;
                }
                else
                {
                    TimeEntries.UpdateItem(update.Id, timeEntryViewModel);
                }
            }
        }

        private void onTimeEntryAdded(IThreadSafeTimeEntry timeEntry)
        {
            var timeEntryViewModel = new TimeEntryViewModel(timeEntry, durationFormat);
            TimeEntries.InsertItem(timeEntryViewModel);
        }

        private void onTimeEntryRemoved(long id)
        {
            var index = TimeEntries.IndexOf(id);
            if (index.HasValue)
                TimeEntries.RemoveItemAt(index.Value.Section, index.Value.Row);
        }

        private void onPreferencesChanged(IThreadSafePreferences preferences)
        {
            if (durationFormat != preferences.DurationFormat)
            {
                durationFormat = preferences.DurationFormat;
                var _ = fetchSectionedTimeEntries();
            }
        }

        private bool isNotRunning(IThreadSafeTimeEntry timeEntry) => !timeEntry.IsRunning();
    }
}
