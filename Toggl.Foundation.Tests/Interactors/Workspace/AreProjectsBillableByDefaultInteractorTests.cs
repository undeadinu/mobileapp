using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors
{
    public class AreProjectsBillableByDefaultInteractorTests
    {
        public sealed class TheAreProjectsBillableByDefaultInteractor : BaseInteractorTests
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ChecksIfTheWorkspacesBillableByDefaultPropertyIfTheWorkspaceIsPro(bool billableByDefault)
            {
                const long workspaceId = 11;
                var workspace = new MockWorkspace { ProjectsBillableByDefault = billableByDefault };
                var feature = new MockWorkspaceFeature { Enabled = true, FeatureId = WorkspaceFeatureId.Pro };
                var featureCollection = new MockWorkspaceFeatureCollection { Features = new[] { feature } };
                InteractorFactory.GetWorkspaceFeaturesById(workspaceId)
                    .Execute()
                    .Returns(Observable.Return(featureCollection));
                InteractorFactory.GetWorkspaceById(workspaceId)
                    .Execute()
                    .Returns(Observable.Return(workspace));

                var projectsAreBillableByDefault =
                    await InteractorFactory.AreProjectsBillableByDefault(workspaceId).Execute();

                projectsAreBillableByDefault.Should().Be(billableByDefault);
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ReturnsNullIfTheWorkspaceIsNotPro(bool billableByDefault)
            {
                const long workspaceId = 11;
                var workspace = new MockWorkspace { ProjectsBillableByDefault = billableByDefault };
                var feature = new MockWorkspaceFeature { Enabled = false, FeatureId = WorkspaceFeatureId.Pro };
                var featureCollection = new MockWorkspaceFeatureCollection { Features = new[] { feature } };
                InteractorFactory.GetWorkspaceFeaturesById(workspaceId)
                    .Execute()
                    .Returns(Observable.Return(featureCollection));
                InteractorFactory.GetWorkspaceById(workspaceId)
                    .Execute()
                    .Returns(Observable.Return(workspace));

                var projectsAreBillableByDefault =
                    await InteractorFactory.AreProjectsBillableByDefault(workspaceId).Execute();

                projectsAreBillableByDefault.Should().Be(null);
            }
        }
    }
}
