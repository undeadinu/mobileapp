using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Xunit;
using Toggl.Multivac.Extensions;
using System.Reactive.Linq;
using Toggl.Foundation.Interactors;
using System.Reactive;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Exceptions;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectDefaultWorkspaceViewModelTests
    {
        public abstract class SelectDefaultWorkspaceViewModelTest : BaseViewModelTests<SelectDefaultWorkspaceViewModel>
        {
            protected override SelectDefaultWorkspaceViewModel CreateViewModel()
                => new SelectDefaultWorkspaceViewModel(DataSource, InteractorFactory, NavigationService, AccessRestrictionStorage, RxActionFactory);
        }

        public sealed class TheConstructor : SelectDefaultWorkspaceViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useAccessRestrictionStorage,
                bool useRxActionFactory)
            {
                Action tryingToConstructWithEmptyParameters = ()
                    => new SelectDefaultWorkspaceViewModel(
                        useDataSource ? DataSource : null,
                        useInteractorFactory ? InteractorFactory : null,
                        useNavigationService ? NavigationService : null,
                        useAccessRestrictionStorage ? AccessRestrictionStorage : null,
                        useRxActionFactory ? RxActionFactory : null);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitializeMethod : SelectDefaultWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task FillsTheWorkspaceList()
            {
                var workspaceCount = 10;
                var workspaceIds = Enumerable
                    .Range(0, workspaceCount)
                    .Select(id => (long)id);
                var workspaces = workspaceIds
                    .Select(id => new MockWorkspace { Id = id, Name = id.ToString() })
                    .Apply(Observable.Return);
                DataSource.Workspaces.GetAll().Returns(workspaces);

                await ViewModel.Initialize();

                ViewModel
                    .Workspaces
                    .Should()
                    .OnlyContain(workspace => workspaceIds.Contains(workspace.WorkspaceId));
            }

            [Fact, LogIfTooSlow]
            public void ThrowsNoWorkspaceExceptionIfThereAreNoWorkspaces()
            {
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new IThreadSafeWorkspace[0]));

                Action initialization = () => ViewModel.Initialize().Wait();

                initialization.Should().Throw<NoWorkspaceException>();
            }
        }

        public sealed class SelectWorkspaceAction : SelectDefaultWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task SetsTheWorkspaceAsDefault()
            {
                var workspaceCount = 10;
                var workspaceIds = Enumerable
                    .Range(0, workspaceCount)
                    .Select(id => (long)id);
                var workspaces = workspaceIds
                    .Select(id => new MockWorkspace { Id = id, Name = id.ToString() })
                    .Apply(Observable.Return);
                DataSource.Workspaces.GetAll().Returns(workspaces);
                var setDefaultWorkspaceInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                InteractorFactory.SetDefaultWorkspace(Arg.Any<long>()).Returns(setDefaultWorkspaceInteractor);
                await ViewModel.Initialize();
                var selectedWorkspace = ViewModel.Workspaces.First();

                ViewModel.SelectWorkspace.Execute(selectedWorkspace);
                TestScheduler.Start();

                InteractorFactory.Received().SetDefaultWorkspace(selectedWorkspace.WorkspaceId);
                await setDefaultWorkspaceInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                var selectedWorkspace = new SelectableWorkspaceViewModel(new MockWorkspace(), false);

                ViewModel.SelectWorkspace.Execute(selectedWorkspace);
                TestScheduler.Start();

                await NavigationService.Received().Close(ViewModel, Unit.Default);
            }
        }
    }
}
