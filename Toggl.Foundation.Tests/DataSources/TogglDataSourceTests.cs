using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Helpers;
using Toggl.Foundation.Tests.Sync;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using ThreadingTask = System.Threading.Tasks.Task;
using Toggl.Ultrawave;
using Toggl.Foundation.Shortcuts;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class TogglDataSourceTests
    {
        public abstract class TogglDataSourceTest
        {
            protected ITogglDataSource DataSource { get; }
            protected ITogglApi Api { get; } = Substitute.For<ITogglApi>();
            protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
            protected ISyncManager SyncManager { get; } = Substitute.For<ISyncManager>();
            protected INotificationService NotificationService { get; } = Substitute.For<INotificationService>();
            protected ISubject<SyncProgress> ProgressSubject = new Subject<SyncProgress>();
            protected IApplicationShortcutCreator ApplicationShortcutCreator { get; } = Substitute.For<IApplicationShortcutCreator>();
            protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();

            public TogglDataSourceTest()
            {
                SyncManager.ProgressObservable.Returns(ProgressSubject.AsObservable());
                DataSource = new TogglDataSource(
                    Api,
                    Database,
                    TimeService,
                    _ => SyncManager,
                    NotificationService,
                    ApplicationShortcutCreator,
                    AnalyticsService);
            }
        }

        public sealed class TheLogoutMethod : TogglDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public void ClearsTheDatabase()
            {
                DataSource.Logout().Wait();

                Database.Received(1).Clear();
            }

            [Fact, LogIfTooSlow]
            public void FreezesTheSyncManager()
            {
                DataSource.Logout().Wait();

                SyncManager.Received().Freeze();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotClearTheDatabaseBeforeTheSyncManagerCompletesFreezing()
            {
                var scheduler = new TestScheduler();
                SyncManager.Freeze().Returns(Observable.Never<SyncState>());

                var observable = DataSource.Logout().SubscribeOn(scheduler).Publish();
                observable.Connect();
                scheduler.AdvanceBy(TimeSpan.FromDays(1).Ticks);

                Database.DidNotReceive().Clear();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UnschedulesAllNotifications()
            {
                await DataSource.Logout();

                await NotificationService.Received().UnscheduleAllNotifications();
            }

            [Fact, LogIfTooSlow]
            public void ClearTheDatabaseOnlyOnceTheSyncManagerFreezeEmitsAValueEvenThoughItDoesNotComplete()
            {
                var freezingSubject = new Subject<SyncState>();
                SyncManager.Freeze().Returns(freezingSubject.AsObservable());

                var observable = DataSource.Logout().Publish();
                observable.Connect();

                Database.DidNotReceive().Clear();

                freezingSubject.OnNext(SyncState.Sleep);

                Database.Received().Clear();
            }

            [Fact, LogIfTooSlow]
            public void EmitsUnitValueAndCompletesWhenFreezeAndDatabaseClearEmitSingleValueButDoesNotComplete()
            {
                var clearingSubject = new Subject<Unit>();
                SyncManager.Freeze().Returns(_ => Observable.Return(SyncState.Sleep));
                Database.Clear().Returns(clearingSubject.AsObservable());
                bool emitsUnitValue = false;
                bool completed = false;

                var observable = DataSource.Logout();
                observable.Subscribe(
                    _ => emitsUnitValue = true,
                    () => completed = true);
                clearingSubject.OnNext(Unit.Default);

                emitsUnitValue.Should().BeTrue();
                completed.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask NotifiesShortcutCreatorAboutLogout()
            {
                await DataSource.Logout();

                ApplicationShortcutCreator.Received().OnLogout();
            }
        }

        public sealed class TheHasUnsyncedDataMetod
        {
            public abstract class BaseHasUnsyncedDataTest<TModel, TDatabaseModel> : TogglDataSourceTest
                where TModel : class
                where TDatabaseModel : TModel, IDatabaseSyncable
            {
                private readonly IBaseStorage<TDatabaseModel> repository;
                private readonly Func<TModel, TDatabaseModel> dirty;
                private readonly Func<TModel, string, TDatabaseModel> unsyncable;

                public BaseHasUnsyncedDataTest(Func<ITogglDatabase, IBaseStorage<TDatabaseModel>> repository,
                    Func<TModel, TDatabaseModel> dirty, Func<TModel, string, TDatabaseModel> unsyncable)
                {
                    this.repository = repository(Database);
                    this.dirty = dirty;
                    this.unsyncable = unsyncable;
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask ReturnsTrueWhenThereIsAnEntityWhichNeedsSync()
                {
                    var dirtyEntity = dirty(Substitute.For<TModel>());
                    repository.GetAll(Arg.Any<Func<TDatabaseModel, bool>>())
                        .Returns(Observable.Return(new[] { dirtyEntity }));

                    var hasUnsyncedData = await DataSource.HasUnsyncedData();

                    hasUnsyncedData.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask ReturnsTrueWhenThereIsAnEntityWhichFailedToSync()
                {
                    var unsyncableEntity = unsyncable(Substitute.For<TModel>(), "Error message.");
                    repository.GetAll(Arg.Any<Func<TDatabaseModel, bool>>())
                        .Returns(Observable.Return(new[] { unsyncableEntity }));

                    var hasUnsyncedData = await DataSource.HasUnsyncedData();

                    hasUnsyncedData.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask ReturnsFalseWhenThereIsNoUnsyncedEntityAndAllOtherRepositoriesAreSyncedAsWell()
                {
                    repository.GetAll(Arg.Any<Func<TDatabaseModel, bool>>())
                        .Returns(Observable.Return(new TDatabaseModel[0]));

                    var hasUnsyncedData = await DataSource.HasUnsyncedData();

                    hasUnsyncedData.Should().BeFalse();
                }
            }

            public sealed class TimeEntriesTest : BaseHasUnsyncedDataTest<ITimeEntry, IDatabaseTimeEntry>
            {
                public TimeEntriesTest()
                    : base(database => database.TimeEntries, TimeEntry.Dirty, TimeEntry.Unsyncable)
                {
                }
            }

            public sealed class ProjectsTest : BaseHasUnsyncedDataTest<IProject, IDatabaseProject>
            {
                public ProjectsTest()
                    : base(database => database.Projects, Project.Dirty, Project.Unsyncable)
                {
                }
            }

            public sealed class UserTest : BaseHasUnsyncedDataTest<IUser, IDatabaseUser>
            {
                public UserTest()
                    : base(database => database.User, User.Dirty, User.Unsyncable)
                {
                }
            }

            public sealed class TasksTest : BaseHasUnsyncedDataTest<ITask, IDatabaseTask>
            {
                public TasksTest()
                    : base(database => database.Tasks, Task.Dirty, Task.Unsyncable)
                {
                }
            }

            public sealed class ClientsTest : BaseHasUnsyncedDataTest<IClient, IDatabaseClient>
            {
                public ClientsTest()
                    : base(database => database.Clients, Client.Dirty, Client.Unsyncable)
                {
                }
            }

            public sealed class TagsTest : BaseHasUnsyncedDataTest<ITag, IDatabaseTag>
            {
                public TagsTest()
                    : base(database => database.Tags, Tag.Dirty, Tag.Unsyncable)
                {
                }
            }

            public sealed class WorkspacesTest : BaseHasUnsyncedDataTest<IWorkspace, IDatabaseWorkspace>
            {
                public WorkspacesTest()
                    : base(database => database.Workspaces, Workspace.Dirty, Workspace.Unsyncable)
                {
                }
            }
        }
    }
}
