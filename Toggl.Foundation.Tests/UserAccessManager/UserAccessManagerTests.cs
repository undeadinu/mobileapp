using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Services;
using Toggl.Foundation.Login;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;
using Xunit.Sdk;
using FoundationUser = Toggl.Foundation.Models.User;
using User = Toggl.Ultrawave.Models.User;

namespace Toggl.Foundation.Tests.Login
{
    public sealed class UserAccessManagerTests
    {
        public abstract class UserAccessManagerTest
        {
            protected static readonly Password Password = "theirobotmoviesucked123".ToPassword();
            protected static readonly Email Email = "susancalvin@psychohistorian.museum".ToEmail();
            protected static readonly bool TermsAccepted = true;
            protected static readonly int CountryId = 237;

            protected IUser User { get; } = new User { Id = 10, ApiToken = "ABCDEFG" };
            protected ITogglApi Api { get; } = Substitute.For<ITogglApi>();
            protected IApiFactory ApiFactory { get; } = Substitute.For<IApiFactory>();
            protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();
            protected IGoogleService GoogleService { get; } = Substitute.For<IGoogleService>();
            protected IAccessRestrictionStorage AccessRestrictionStorage { get; } = Substitute.For<IAccessRestrictionStorage>();
            protected ITogglDataSource DataSource { get; } = Substitute.For<ITogglDataSource>();
            protected IApplicationShortcutCreator ApplicationShortcutCreator { get; } = Substitute.For<IApplicationShortcutCreator>();
            protected readonly IUserAccessManager UserAccessManager;
            protected IScheduler Scheduler { get; } = System.Reactive.Concurrency.Scheduler.Default;
            protected ITogglDataSource CreateDataSource(ITogglApi api) => DataSource;
            protected virtual IScheduler CreateScheduler => Scheduler;
            protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();
            protected IPrivateSharedStorageService PrivateSharedStorageService { get; } = Substitute.For<IPrivateSharedStorageService>();

            protected UserAccessManagerTest()
            {
                UserAccessManager = new UserAccessManager(ApiFactory, Database, GoogleService, ApplicationShortcutCreator, PrivateSharedStorageService, CreateDataSource);

                Api.User.Get().Returns(Observable.Return(User));
                Api.User.SignUp(Email, Password, TermsAccepted, CountryId).Returns(Observable.Return(User));
                Api.User.GetWithGoogle().Returns(Observable.Return(User));
                ApiFactory.CreateApiWith(Arg.Any<Credentials>()).Returns(Api);
                Database.Clear().Returns(Observable.Return(Unit.Default));
            }
        }

        public abstract class UserAccessManagerWithTestSchedulerTest : UserAccessManagerTest
        {
            protected readonly TestScheduler TestScheduler = new TestScheduler();
            protected override IScheduler CreateScheduler => TestScheduler;
        }

        public sealed class Constructor : UserAccessManagerTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useApiFactory,
                bool useDatabase,
                bool useGoogleService,
                bool useApplicationShortcutCreator,
                bool useCreateDataSource,
                bool usePrivateSharedStorageService)
            {
                var database = useDatabase ? Database : null;
                var apiFactory = useApiFactory ? ApiFactory : null;
                var googleService = useGoogleService ? GoogleService : null;
                var createDataSource = useCreateDataSource ? CreateDataSource : (Func<ITogglApi, ITogglDataSource>)null;
                var shortcutCreator = useApplicationShortcutCreator ? ApplicationShortcutCreator : null;
                var privateSharedStorageService = usePrivateSharedStorageService ? PrivateSharedStorageService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new UserAccessManager(apiFactory, database, googleService, shortcutCreator, privateSharedStorageService, createDataSource);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheUserAccessMethod : UserAccessManagerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("susancalvin@psychohistorian.museum", null)]
            [InlineData("susancalvin@psychohistorian.museum", "")]
            [InlineData("susancalvin@psychohistorian.museum", " ")]
            [InlineData("susancalvin@", null)]
            [InlineData("susancalvin@", "")]
            [InlineData("susancalvin@", " ")]
            [InlineData("susancalvin@", "123456")]
            [InlineData("", null)]
            [InlineData("", "")]
            [InlineData("", " ")]
            [InlineData("", "123456")]
            [InlineData(null, null)]
            [InlineData(null, "")]
            [InlineData(null, " ")]
            [InlineData(null, "123456")]
            public void ThrowsIfYouPassInvalidParameters(string email, string password)
            {
                var actualEmail = email.ToEmail();
                var actualPassword = password.ToPassword();

                Action tryingToConstructWithEmptyParameters =
                    () => UserAccessManager.Login(actualEmail, actualPassword).Wait();

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public async Task EmptiesTheDatabaseBeforeTryingToLogin()
            {
                await UserAccessManager.Login(Email, Password);

                Received.InOrder(async () =>
                {
                    await Database.Clear();
                    await Api.User.Get();
                });
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheGetMethodOfTheUserApi()
            {
                await UserAccessManager.Login(Email, Password);

                await Api.User.Received().Get();
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserToTheDatabase()
            {
                await UserAccessManager.Login(Email, Password);

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.Id == User.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserWithTheSyncStatusSetToInSync()
            {
                await UserAccessManager.Login(Email, Password);

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.SyncStatus == SyncStatus.InSync));
            }

            [Fact, LogIfTooSlow]
            public async Task AlwaysReturnsASingleResult()
            {
                await UserAccessManager
                        .Login(Email, Password)
                        .SingleAsync();
            }

            [Fact, LogIfTooSlow]
            public async Task NotifiesShortcutCreatorAboutLogin()
            {
                await UserAccessManager.Login(Email, Password);

                ApplicationShortcutCreator.Received().OnLogin(Arg.Any<ITogglDataSource>());
            }

            [Fact, LogIfTooSlow]
            public void DoesNotRetryWhenTheApiThrowsSomethingOtherThanUserIsMissingApiTokenException()
            {
                var serverErrorException = Substitute.For<ServerErrorException>(Substitute.For<IRequest>(), Substitute.For<IResponse>(), "Some Exception");
                Api.User.Get().Returns(Observable.Throw<IUser>(serverErrorException));

                Action tryingToLoginWhenTheApiIsThrowingSomeRandomServerErrorException =
                    () => UserAccessManager.Login(Email, Password).Wait();

                tryingToLoginWhenTheApiIsThrowingSomeRandomServerErrorException
                    .Should().Throw<ServerErrorException>();
            }
        }

        public sealed class TheResetPasswordMethod : UserAccessManagerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("foo")]
            public void ThrowsWhenEmailIsInvalid(string emailString)
            {
                Action tryingToResetWithInvalidEmail = () => UserAccessManager
                    .ResetPassword(Email.From(emailString))
                    .Wait();

                tryingToResetWithInvalidEmail.Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public async Task UsesApiWithoutCredentials()
            {
                await UserAccessManager.ResetPassword(Email.From("some@email.com"));

                ApiFactory.Received().CreateApiWith(Arg.Is<Credentials>(
                    arg => arg.Header.Name == null
                        && arg.Header.Value == null
                        && arg.Header.Type == HttpHeader.HeaderType.None));
            }

            [Theory, LogIfTooSlow]
            [InlineData("example@email.com")]
            [InlineData("john.smith@gmail.com")]
            [InlineData("h4cker123@domain.ru")]
            public async Task CallsApiClientWithThePassedEmailAddress(string address)
            {
                var email = address.ToEmail();

                await UserAccessManager.ResetPassword(email);

                await Api.User.Received().ResetPassword(Arg.Is(email));
            }
        }

        public sealed class TheLogoutMethod : UserAccessManagerTest
        {
            [Fact, LogIfTooSlow]
            public async Task LogsOutAfterLogin()
            {
                await UserAccessManager.Login(Email, Password);
                await UserAccessManager.Logout();
                await DataSource.Received().Logout();
            }

            [Fact, LogIfTooSlow]
            public async Task LogsOutAfterSignup()
            {
                await UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId);
                await UserAccessManager.Logout();
                await DataSource.Received().Logout();
            }

            [Fact, LogIfTooSlow]
            public async Task LogsOutAfterTokenRefresh()
            {
                var user = Substitute.For<IDatabaseUser>();
                user.Email.Returns(Email);
                Database.User.Single().Returns(Observable.Return(user));
                await UserAccessManager.RefreshToken(Password);
                await UserAccessManager.Logout();
                await DataSource.Received().Logout();
            }

            [Fact, LogIfTooSlow]
            public async Task LogsOutAfterGoogleLogin()
            {
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
                await UserAccessManager.LoginWithGoogle();
                await UserAccessManager.Logout();
                await DataSource.Received().Logout();
            }
        }

        public sealed class TheGetDataSourceIfLoggedInInMethod : UserAccessManagerTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsNullIfTheDatabaseHasNoUsers()
            {
                var observable = Observable.Throw<IDatabaseUser>(new InvalidOperationException());
                Database.User.Single().Returns(observable);

                var result = UserAccessManager.GetDataSourceIfLoggedIn();

                result.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsADataSourceIfTheUserExistsInTheDatabase()
            {
                var observable = Observable.Return<IDatabaseUser>(FoundationUser.Clean(User));
                Database.User.Single().Returns(observable);

                var result = UserAccessManager.GetDataSourceIfLoggedIn();

                result.Should().NotBeNull();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotEmitTheDataSourceWhenUserIsNotLoggedIn()
            {
                var observer = Substitute.For<IObserver<ITogglDataSource>>();
                var observable = Observable.Throw<IDatabaseUser>(new InvalidOperationException());
                Database.User.Single().Returns(observable);
                UserAccessManager.UserLoggedIn.Subscribe(observer);

                UserAccessManager.GetDataSourceIfLoggedIn();

                observer.DidNotReceive().OnNext(Arg.Any<ITogglDataSource>());
            }

            [Fact, LogIfTooSlow]
            public void EmitsTheDataSourceWhenUserIsLoggedIn()
            {
                var observer = Substitute.For<IObserver<ITogglDataSource>>();
                var observable = Observable.Return<IDatabaseUser>(FoundationUser.Clean(User));
                Database.User.Single().Returns(observable);
                UserAccessManager.UserLoggedIn.Subscribe(observer);

                UserAccessManager.GetDataSourceIfLoggedIn();

                observer.Received().OnNext(Arg.Any<ITogglDataSource>());
            }
        }

        public sealed class TheSignUpMethod : UserAccessManagerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("susancalvin@psychohistorian.museum", null)]
            [InlineData("susancalvin@psychohistorian.museum", "")]
            [InlineData("susancalvin@psychohistorian.museum", " ")]
            [InlineData("susancalvin@", null)]
            [InlineData("susancalvin@", "")]
            [InlineData("susancalvin@", " ")]
            [InlineData("susancalvin@", "123456")]
            [InlineData("", null)]
            [InlineData("", "")]
            [InlineData("", " ")]
            [InlineData("", "123456")]
            [InlineData(null, null)]
            [InlineData(null, "")]
            [InlineData(null, " ")]
            [InlineData(null, "123456")]
            public void ThrowsIfYouPassInvalidParameters(string email, string password)
            {
                var actualEmail = email.ToEmail();
                var actualPassword = password.ToPassword();

                Action tryingToConstructWithEmptyParameters =
                    () => UserAccessManager.SignUp(actualEmail, actualPassword, true, 0).Wait();

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public async Task EmptiesTheDatabaseBeforeTryingToCreateTheUser()
            {
                await UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId);

                Received.InOrder(async () =>
                {
                    await Database.Clear();
                    await Api.User.SignUp(Email, Password, TermsAccepted, CountryId);
                });
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheSignUpMethodOfTheUserApi()
            {
                await UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId);

                await Api.User.Received().SignUp(Email, Password, TermsAccepted, CountryId);
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserToTheDatabase()
            {
                await UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId);

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.Id == User.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserWithTheSyncStatusSetToInSync()
            {
                await UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId);

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.SyncStatus == SyncStatus.InSync));
            }

            [Fact, LogIfTooSlow]
            public async Task AlwaysReturnsASingleResult()
            {
                await UserAccessManager
                        .SignUp(Email, Password, TermsAccepted, CountryId)
                        .SingleAsync();
            }

            [Fact, LogIfTooSlow]
            public async Task NotifiesShortcutCreatorAboutLogin()
            {
                await UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId);

                ApplicationShortcutCreator.Received().OnLogin(Arg.Any<ITogglDataSource>());
            }

            [Fact, LogIfTooSlow]
            public void DoesNotRetryWhenTheApiThrowsSomethingOtherThanUserIsMissingApiTokenException()
            {
                var serverErrorException = Substitute.For<ServerErrorException>(Substitute.For<IRequest>(), Substitute.For<IResponse>(), "Some Exception");
                Api.User.SignUp(Email, Password, TermsAccepted, CountryId).Returns(Observable.Throw<IUser>(serverErrorException));

                Action tryingToSignUpWhenTheApiIsThrowingSomeRandomServerErrorException =
                    () => UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId).Wait();

                tryingToSignUpWhenTheApiIsThrowingSomeRandomServerErrorException
                    .Should().Throw<ServerErrorException>();
            }

            [Fact, LogIfTooSlow]
            public async Task SavesTheApiTokenToPrivateSharedStorage()
            {
                await UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId);

                PrivateSharedStorageService.Received().SaveApiToken(Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task SavesTheUserIdToPrivateSharedStorage()
            {
                await UserAccessManager.SignUp(Email, Password, TermsAccepted, CountryId);

                PrivateSharedStorageService.Received().SaveUserId(Arg.Any<long>());
            }
        }

        public sealed class TheRefreshTokenMethod : UserAccessManagerTest
        {
            public TheRefreshTokenMethod()
            {
                var user = Substitute.For<IDatabaseUser>();
                user.Email.Returns(Email);
                Database.User.Single().Returns(Observable.Return(user));
            }

            [Theory, LogIfTooSlow]
            [InlineData(null)]
            [InlineData("")]
            [InlineData(" ")]
            public void ThrowsIfYouPassInvalidParameters(string password)
            {
                Action tryingToRefreshWithInvalidParameters =
                    () => UserAccessManager.RefreshToken(password.ToPassword()).Wait();

                tryingToRefreshWithInvalidParameters
                    .Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheGetMethodOfTheUserApi()
            {
                await UserAccessManager.RefreshToken(Password);

                 await Api.User.Received().Get();
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserToTheDatabase()
            {
                await UserAccessManager.RefreshToken(Password);

                await Database.User.Received().Update(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.Id == User.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserWithTheSyncStatusSetToInSync()
            {
                await UserAccessManager.RefreshToken(Password);

                await Database.User.Received().Update(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.SyncStatus == SyncStatus.InSync));
            }

            [Fact, LogIfTooSlow]
            public async Task AlwaysReturnsASingleResult()
            {
                await UserAccessManager
                        .RefreshToken(Password)
                        .SingleAsync();
            }

            [Fact, LogIfTooSlow]
            public async Task SavesTheApiTokenToPrivateSharedStorage()
            {
                await UserAccessManager.RefreshToken(Password);

                PrivateSharedStorageService.Received().SaveApiToken(Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task SavesTheUserIdToPrivateSharedStorage()
            {
                await UserAccessManager.RefreshToken(Password);

                PrivateSharedStorageService.Received().SaveUserId(Arg.Any<long>());
            }
        }

        public sealed class TheUserAccessUsingGoogleMethod : UserAccessManagerTest
        {
            public TheUserAccessUsingGoogleMethod()
            {
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
            }

            [Fact, LogIfTooSlow]
            public async Task EmptiesTheDatabaseBeforeTryingToCreateTheUser()
            {
                await UserAccessManager.LoginWithGoogle();

                Received.InOrder(async () =>
                {
                    await Database.Clear();
                    await Api.User.GetWithGoogle();
                });
            }

            [Fact, LogIfTooSlow]
            public async Task UsesTheGoogleServiceToGetTheToken()
            {
                await UserAccessManager.LoginWithGoogle();

                await GoogleService.Received().GetAuthToken();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheGetWithGoogleOfTheUserApi()
            {
                await UserAccessManager.LoginWithGoogle();

                await Api.User.Received().GetWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserToTheDatabase()
            {
                await UserAccessManager.LoginWithGoogle();

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.Id == User.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserWithTheSyncStatusSetToInSync()
            {
                await UserAccessManager.LoginWithGoogle();

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.SyncStatus == SyncStatus.InSync));
            }

            [Fact, LogIfTooSlow]
            public async Task AlwaysReturnsASingleResult()
            {
                await UserAccessManager
                        .LoginWithGoogle()
                        .SingleAsync();
            }

            [Fact, LogIfTooSlow]
            public async Task NotifiesShortcutCreatorAboutLogin()
            {
                await UserAccessManager.LoginWithGoogle();

                ApplicationShortcutCreator.Received().OnLogin(Arg.Any<ITogglDataSource>());
            }

            [Fact, LogIfTooSlow]
            public async Task SavesTheApiTokenToPrivateSharedStorage()
            {
                await UserAccessManager.LoginWithGoogle();

                PrivateSharedStorageService.Received().SaveApiToken(Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task SavesTheUserIdToPrivateSharedStorage()
            {
                await UserAccessManager.LoginWithGoogle();

                PrivateSharedStorageService.Received().SaveUserId(Arg.Any<long>());
            }
        }
    }
}
