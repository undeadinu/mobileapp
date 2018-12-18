using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Login
{
    public sealed class UserAccessManager : IUserAccessManager
    {
        private readonly IApiFactory apiFactory;
        private readonly ITogglDatabase database;
        private readonly IGoogleService googleService;
        private readonly IApplicationShortcutCreator shortcutCreator;
        private readonly IPrivateSharedStorageService privateSharedStorageService;
        private readonly Func<ITogglApi, ITogglDataSource> createDataSource;

        private ITogglDataSource cachedDataSource;
        private ISubject<ITogglDataSource> userLoggedInSubject = new Subject<ITogglDataSource>();
        private ISubject<Unit> userLoggedOutSubject = new Subject<Unit>();

        public IObservable<ITogglDataSource> UserLoggedIn => userLoggedInSubject.AsObservable();
        public IObservable<Unit> UserLoggedOut => userLoggedOutSubject.AsObservable();

        public UserAccessManager(
            IApiFactory apiFactory,
            ITogglDatabase database,
            IGoogleService googleService,
            IApplicationShortcutCreator shortcutCreator,
            IPrivateSharedStorageService privateSharedStorageService,
            Func<ITogglApi, ITogglDataSource> createDataSource
        )
        {
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(apiFactory, nameof(apiFactory));
            Ensure.Argument.IsNotNull(googleService, nameof(googleService));
            Ensure.Argument.IsNotNull(shortcutCreator, nameof(shortcutCreator));
            Ensure.Argument.IsNotNull(privateSharedStorageService, nameof(privateSharedStorageService));
            Ensure.Argument.IsNotNull(createDataSource, nameof(createDataSource));

            this.database = database;
            this.apiFactory = apiFactory;
            this.googleService = googleService;
            this.privateSharedStorageService = privateSharedStorageService;
            this.shortcutCreator = shortcutCreator;
            this.createDataSource = createDataSource;
        }

        public IObservable<ITogglDataSource> Login(Email email, Password password)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to login");
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to login");

            var credentials = Credentials.WithPassword(email, password);

            return database
                .Clear()
                .SelectMany(_ => apiFactory.CreateApiWith(credentials).User.Get())
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin)
                .Do(userLoggedInSubject.OnNext);
        }

        public IObservable<ITogglDataSource> LoginWithGoogle()
            => database
                .Clear()
                .SelectMany(_ => googleService.LogOutIfNeeded())
                .SelectMany(_ => googleService.GetAuthToken())
                .SelectMany(loginWithGoogle);

        public IObservable<ITogglDataSource> SignUp(Email email, Password password, bool termsAccepted, int countryId)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to signup");
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to signup");

            return database
                .Clear()
                .SelectMany(_ => signUp(email, password, termsAccepted, countryId))
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin);
        }

        public IObservable<ITogglDataSource> SignUpWithGoogle(bool termsAccepted, int countryId)
            => database
                .Clear()
                .SelectMany(_ => googleService.LogOutIfNeeded())
                .SelectMany(_ => googleService.GetAuthToken())
                .SelectMany(authToken => signUpWithGoogle(authToken, termsAccepted, countryId));

        public IObservable<Unit> Logout()
            => cachedDataSource.Logout()
                .Do(_ => userLoggedOutSubject.OnNext(Unit.Default));

        public IObservable<string> ResetPassword(Email email)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to reset forgotten password.");

            var api = apiFactory.CreateApiWith(Credentials.None);
            return api.User.ResetPassword(email);
        }

        public ITogglDataSource GetDataSourceIfLoggedIn()
            => database.User
                .Single()
                .Select(dataSourceFromUser)
                .Do(userLoggedInSubject.OnNext)
                .Catch(Observable.Return<ITogglDataSource>(null))
                .Do(shortcutCreator.OnLogin)
                .Wait();

        public IObservable<ITogglDataSource> RefreshToken(Password password)
        {
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to refresh token");

            return database.User
                .Single()
                .Select(user => user.Email)
                .Select(email => Credentials.WithPassword(email, password))
                .Select(apiFactory.CreateApiWith)
                .SelectMany(api => api.User.Get())
                .Select(User.Clean)
                .SelectMany(database.User.Update)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin)
                .Do(userLoggedInSubject.OnNext);
        }

        private ITogglDataSource dataSourceFromUser(IUser user)
        {
            privateSharedStorageService.SaveApiToken(user.ApiToken);
            privateSharedStorageService.SaveUserId(user.Id);

            var newCredentials = Credentials.WithApiToken(user.ApiToken);
            var api = apiFactory.CreateApiWith(newCredentials);
            cachedDataSource = createDataSource(api);
            return cachedDataSource;
        }

        private IObservable<ITogglDataSource> loginWithGoogle(string googleToken)
        {
            var credentials = Credentials.WithGoogleToken(googleToken);

            return Observable
                .Return(apiFactory.CreateApiWith(credentials))
                .SelectMany(api => api.User.GetWithGoogle())
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin)
                .Do(userLoggedInSubject.OnNext);
        }

        private IObservable<IUser> signUp(Email email, Password password, bool termsAccepted, int countryId)
        {
            return apiFactory
                .CreateApiWith(Credentials.None)
                .User
                .SignUp(email, password, termsAccepted, countryId);
        }


        private IObservable<ITogglDataSource> signUpWithGoogle(string googleToken, bool termsAccepted, int countryId)
        {
            var api = apiFactory.CreateApiWith(Credentials.None);
            return api.User
                .SignUpWithGoogle(googleToken, termsAccepted, countryId)
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin)
                .Do(userLoggedInSubject.OnNext);
        }
    }
}
