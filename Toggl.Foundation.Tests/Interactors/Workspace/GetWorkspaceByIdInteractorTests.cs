using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Tests.Mocks;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors
{
    public class GetWorkspaceByIdInteractorTests
    {
        public sealed class TheGetWorkspaceByIdInteractor : BaseInteractorTests
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsTheWorkspaceWithPassedId()
            {
                const long workspaceId = 10;
                var workspace = new MockWorkspace();
                InteractorFactory.GetWorkspaceById(workspaceId)
                    .Execute()
                    .Returns(Observable.Return(workspace));

                var returnedWorkspace = await InteractorFactory.GetWorkspaceById(workspaceId).Execute();

                returnedWorkspace.Should().BeSameAs(workspace);
            }
        }
    }
}
