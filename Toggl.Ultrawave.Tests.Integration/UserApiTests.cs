using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using FluentAssertions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Tests.Integration.BaseTests;
using Toggl.Ultrawave.Tests.Integration.Helper;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class UserApiTests
    {
        public sealed class TheGetMethod : AuthenticatedEndpointBaseTests<IUser>
        {
            protected override IObservable<IUser> CallEndpointWith(ITogglApi togglApi)
                => togglApi.User.Get();

            [Fact, LogTestInfo]
            public async Task ReturnsValidEmail()
            {
                var (email, password) = await User.CreateEmailPassword();
                var credentials = Credentials.WithPassword(email, password);
                var api = TogglApiWith(credentials);

                var user = await api.User.Get();
                user.Email.Should().Be(email);
            }

            [Fact, LogTestInfo]
            public async Task ReturnsValidId()
            {
                var (togglApi, user) = await SetupTestUser();

                var userFromApi = await CallEndpointWith(togglApi);

                userFromApi.Id.Should().NotBe(0);
            }

            [Fact, LogTestInfo]
            public async Task ReturnsValidApiToken()
            {
                var (togglApi, user) = await SetupTestUser();
                var regex = "^[a-fA-F0-9]+$";

                var userFromApi = await CallEndpointWith(togglApi);

                userFromApi.ApiToken.Should().NotBeNull()
                    .And.HaveLength(32)
                    .And.MatchRegex(regex);
            }

            [Fact, LogTestInfo]
            public async Task ReturnsValidBeginningOfWeek()
            {
                var (togglApi, user) = await SetupTestUser();

                var userFromApi = await CallEndpointWith(togglApi);
                var beginningOfWeekInt = (int)userFromApi.BeginningOfWeek;

                beginningOfWeekInt.Should().BeInRange(0, 6);
            }

            [Fact, LogTestInfo]
            public async Task ReturnsValidDefaultWorkspaceId()
            {
                var (togglApi, user) = await SetupTestUser();

                var userFromApi = await CallEndpointWith(togglApi);
                var workspace = await togglApi.Workspaces.GetById(userFromApi.DefaultWorkspaceId.Value);

                userFromApi.DefaultWorkspaceId.Should().NotBe(0);
                workspace.Should().NotBeNull();
            }

            [Fact, LogTestInfo]
            public async Task ReturnsValidImageUrl()
            {
                var (togglApi, user) = await SetupTestUser();

                var userFromApi = await CallEndpointWith(togglApi);

                userFromApi.ImageUrl.Should().NotBeNullOrEmpty();
                var uri = new Uri(userFromApi.ImageUrl);
                var uriIsAbsolute = uri.IsAbsoluteUri;
                uriIsAbsolute.Should().BeTrue();
            }
        }

        public sealed class TheResetPasswordMethod : EndpointTestBase
        {
            [Theory, LogTestInfo]
            [ClassData(typeof(InvalidEmailTestData))]
            public void ThrowsIfTheEmailIsInvalid(string emailString)
            {
                var api = TogglApiWith(Credentials.None);

                Action resetInvalidEmail = () => api
                    .User
                    .ResetPassword(Email.From(emailString))
                    .Wait();

                resetInvalidEmail.Should().Throw<BadRequestException>();
            }

            [Fact, LogTestInfo]
            public void FailsIfUserDoesNotExist()
            {
                var api = TogglApiWith(Credentials.None);
                var email = RandomEmail.GenerateValid();

                Action resetInvalidEmail = () => api.User.ResetPassword(email).Wait();

                resetInvalidEmail.Should().Throw<BadRequestException>();
            }

            [Fact, LogTestInfo]
            public async Task ReturnsUserFriendlyInstructionsInEnglishWhenResetSucceeds()
            {
                var (_, user) = await SetupTestUser();
                var api = TogglApiWith(Credentials.None);

                var instructions = await api.User.ResetPassword(user.Email);

                instructions.Should().Be("Please check your inbox for further instructions");
            }
        }

        public class TheSignUpMethod : EndpointTestBase
        {
            private readonly ITogglApi unauthenticatedTogglApi;

            public TheSignUpMethod()
            {
                unauthenticatedTogglApi = TogglApiWith(Credentials.None);
            }

            [Fact, LogTestInfo]
            public void ThrowsIfTheEmailIsEmpty()
            {
                Action signingUp = () => unauthenticatedTogglApi
                    .User
                    .SignUp(Email.Empty, "dummyButValidPassword".ToPassword(), true, 237)
                    .Wait();

                signingUp.Should().Throw<ArgumentException>();
            }

            [Theory, LogTestInfo]
            [ClassData(typeof(InvalidEmailTestData))]
            public void ThrowsWhenTheEmailIsNotValid(string emailString)
            {
                Action signingUp = () => unauthenticatedTogglApi
                    .User
                    .SignUp(Email.From(emailString), "dummyButValidPassword".ToPassword(), true, 237)
                    .Wait();

                signingUp.Should().Throw<ArgumentException>();
            }

            [Theory, LogTestInfo]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData("\t")]
            [InlineData(" \t ")]
            [InlineData("\n")]
            [InlineData(" \n ")]
            [InlineData(" \t\n ")]
            [InlineData("xyz")]
            [InlineData("12345")]
            [InlineData("1@bX_")]
            public void FailsWhenThePasswordIsTooShort(string empty)
            {
                Action signingUp = () => unauthenticatedTogglApi
                    .User
                    .SignUp(RandomEmail.GenerateValid(), empty.ToPassword(), true, 237)
                    .Wait();

                signingUp.Should().Throw<BadRequestException>();
            }

            [Theory, LogTestInfo]
            [InlineData("  \t   ")]
            [InlineData("  \t\n  ")]
            [InlineData("\n\n\n\n\n\n")]
            [InlineData("            ")]
            public async Task SucceedsForAPasswordConsistingOfOnlyWhiteCharactersWhenItIsLongEnough(string seeminglyEmpty)
            {
                var email = RandomEmail.GenerateValid();

                var user = await unauthenticatedTogglApi
                    .User
                    .SignUp(email, seeminglyEmpty.ToPassword(), true, 237);

                user.Id.Should().BeGreaterThan(0);
                user.Email.Should().Be(email);
            }

            [Fact, LogTestInfo]
            public async Task CreatesANewUserAccount()
            {
                var emailAddress = RandomEmail.GenerateValid();

                var user = await unauthenticatedTogglApi
                    .User
                    .SignUp(emailAddress, "somePassword".ToPassword(), true, 237);

                user.Email.Should().Be(emailAddress);
            }

            [Fact, LogTestInfo]
            public async Task FailsWhenTheEmailIsAlreadyTaken()
            {
                var email = RandomEmail.GenerateValid();
                await unauthenticatedTogglApi.User.SignUp(email, "somePassword".ToPassword(), true, 237);

                Action secondSigningUp = () => unauthenticatedTogglApi
                    .User
                    .SignUp(email, "thePasswordIsNotImportant".ToPassword(), true, 237)
                    .Wait();

                secondSigningUp.Should().Throw<EmailIsAlreadyUsedException>();
            }

            [Fact, LogTestInfo]
            public async Task FailsWhenSigningUpWithTheSameEmailAndPasswordForTheSecondTime()
            {
                var email = RandomEmail.GenerateValid();
                var password = "somePassword".ToPassword();
                await unauthenticatedTogglApi.User.SignUp(email, password, true, 237);

                Action secondSigningUp = () => unauthenticatedTogglApi.User.SignUp(email, password, true, 237).Wait();

                secondSigningUp.Should().Throw<EmailIsAlreadyUsedException>();
            }

            [Fact, LogTestInfo]
            public async Task EnablesLoginForTheNewlyCreatedUserAccount()
            {
                var emailAddress = RandomEmail.GenerateValid();
                var password = Guid.NewGuid().ToString().ToPassword();

                var signedUpUser = await unauthenticatedTogglApi.User.SignUp(emailAddress, password, true, 237);
                var credentials = Credentials.WithPassword(emailAddress, password);
                var togglApi = TogglApiWith(credentials);
                var user = await togglApi.User.Get();

                signedUpUser.Id.Should().Be(user.Id);
            }

            [Theory, LogTestInfo]
            [InlineData("daneel.olivaw", "Daneel Olivaw's workspace")]
            [InlineData("john.doe", "John Doe's workspace")]
            [InlineData("žížala", "Žížala's workspace")]
            public async Task CreatesADefaultWorkspaceWithCorrectName(string emailPrefix, string expectedWorkspaceName)
            {
                var email = Email.From($"{emailPrefix}@{Guid.NewGuid().ToString()}.com");
                var password = Guid.NewGuid().ToString().ToPassword();

                var user = await unauthenticatedTogglApi.User.SignUp(email, password, true, 237);
                var credentials = Credentials.WithPassword(email, password);
                var togglApi = TogglApiWith(credentials);
                var workspace = await togglApi.Workspaces.GetById(user.DefaultWorkspaceId.Value);

                workspace.Id.Should().BeGreaterThan(0);
                workspace.Name.Should().Be(expectedWorkspaceName);
            }

            [Fact, LogTestInfo]
            public void FailsIfUserDidNotAcceptTermsAndConditions()
            {
                var email = RandomEmail.GenerateValid();
                var password = "s3cr3tzzz".ToPassword();
                var termsAccepted = false;
                var countryId = 237;

                Action signingUpWithoutAcceptingTerms =
                    () => unauthenticatedTogglApi.User
                        .SignUp(email, password, termsAccepted, countryId)
                        .Wait();

                signingUpWithoutAcceptingTerms.Should().Throw<BadRequestException>();
            }

            [Theory, LogTestInfo]
            [InlineData(1)]
            [InlineData(20)]
            [InlineData(237)]
            [InlineData(250)]
            public async Task SucceedsForValidCountryId(int countryId)
            {
                var email = RandomEmail.GenerateValid();
                var password = "s3cr3tzzz".ToPassword();
                var termsNotAccepted = true;

                var user = await unauthenticatedTogglApi
                    .User
                    .SignUp(email, password, termsNotAccepted, countryId);

                user.Id.Should().BeGreaterThan(0);
                user.Email.Should().Be(email);
            }

            [Theory, LogTestInfo]
            [InlineData(0)]
            [InlineData(-1)]
            [InlineData(251)]
            [InlineData(1111111)]
            public void FailsIfCountryIdIsNotValid(int countryId)
            {
                var email = RandomEmail.GenerateValid();
                var password = "s3cr3tzzz".ToPassword();

                Action signingUp = () => unauthenticatedTogglApi
                    .User
                    .SignUp(email, password, true, countryId)
                    .Wait();

                signingUp.Should().Throw<BadRequestException>();
            }
        }

        public class TheSignUpWithGoogleMethod : EndpointTestBase
        {
            private readonly ITogglApi unauthenticatedTogglApi;

            public TheSignUpWithGoogleMethod()
            {
                unauthenticatedTogglApi = TogglApiWith(Credentials.None);
            }

            [Fact, LogTestInfo]
            public void ThrowsIfTheGoogleTokenIsNull()
            {
                Action signingUp = () => unauthenticatedTogglApi
                    .User
                    .SignUpWithGoogle(null, true, 237)
                    .Wait();

                signingUp.Should().Throw<ArgumentException>();
            }


            [Fact, LogTestInfo]
            public void FailsWithUnauthorizedErrorWhenTheGoogleTokenIsEmpty()
            {
                Action signUp = () => unauthenticatedTogglApi
                    .User
                    .SignUpWithGoogle(string.Empty, true, 237)
                    .Wait();

                signUp.Should().Throw<UnauthorizedException>();
            }

            [Theory, LogTestInfo]
            [InlineData("x.y.z")]
            [InlineData("asdkjasdkhjdsadhkda")]
            public void FailsWithServiceUnavailableErrorWhenTheGoogleTokenIsARandomNonEmptyString(string notAToken)
            {
                Action signUp = () => unauthenticatedTogglApi
                    .User
                    .SignUpWithGoogle(notAToken, true, 237)
                    .Wait();

                signUp.Should().Throw<ServiceUnavailableException>();
            }

            [Fact, LogTestInfo]
            public void FailsWhenTheGoogleTokenParameterIsAnInvalidJWT()
            {
                var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWV9.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ";

                Action signUp = () => unauthenticatedTogglApi
                    .User
                    .SignUpWithGoogle(jwt, true, 237)
                    .Wait();

                signUp.Should().Throw<ServiceUnavailableException>();
            }
        }

        public sealed class TheUpdateMethod : AuthenticatedPutEndpointBaseTests<IUser>
        {
            [Fact, LogTestInfo]
            public async Task ChangesDefaultWorkspace()
            {
                var (togglClient, user) = await SetupTestUser();
                var secondWorkspace = await togglClient.Workspaces.Create(new Workspace { Name = Guid.NewGuid().ToString() });

                var userWithUpdates = new Ultrawave.Models.User(user);
                userWithUpdates.DefaultWorkspaceId = secondWorkspace.Id;

                var updatedUser = await togglClient.User.Update(userWithUpdates);

                updatedUser.Id.Should().Be(user.Id);
                updatedUser.DefaultWorkspaceId.Should().NotBe(user.DefaultWorkspaceId);
                updatedUser.DefaultWorkspaceId.Should().Be(secondWorkspace.Id);
            }

            [Fact, LogTestInfo]
            public async Task DoesNotChangeDefaultWorkspaceWhenTheValueIsNull()
            {
                var (togglClient, user) = await SetupTestUser();

                var userWithUpdates = new Ultrawave.Models.User(user);
                userWithUpdates.DefaultWorkspaceId = null;

                var updatedUser = await togglClient.User.Update(userWithUpdates);

                updatedUser.Id.Should().Be(user.Id);
                updatedUser.DefaultWorkspaceId.Should().NotBeNull();
                updatedUser.DefaultWorkspaceId.Should().Be(user.DefaultWorkspaceId);
            }

            protected override IObservable<IUser> PrepareForCallingUpdateEndpoint(ITogglApi api)
                => api.User.Get();

            protected override IObservable<IUser> CallUpdateEndpoint(ITogglApi api, IUser entityToUpdate)
            {
                var entityWithUpdates = new Ultrawave.Models.User(entityToUpdate);
                entityWithUpdates.Fullname = entityToUpdate.Fullname == "Test" ? "Different name" : "Test";

                return api.User.Update(entityWithUpdates);
            }
        }

        private sealed class InvalidEmailTestData : IEnumerable<object[]>
        {
            private List<object[]> emailStrings;

            public InvalidEmailTestData()
            {
                emailStrings = new List<object[]>
                {
                    new[] { "" },
                    new[] { "not an email" },
                    new[] { "em@il" },
                    new[] { "domain.com" },
                    new[] { "thisIsNotAnEmail@.com" },
                    new[] { "alsoNot@email." },
                    new[] { "double@at@email.com" },
                    new[] { "so#close@yet%so.far" }
                };
            }

            public IEnumerator<object[]> GetEnumerator()
                => emailStrings.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
