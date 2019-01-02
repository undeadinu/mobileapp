using System;
using System.Reactive;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;

namespace Toggl.Foundation.Login
{
    public interface IUserAccessManager
    {
        IObservable<ITogglDataSource> UserLoggedIn { get; }
        IObservable<Unit> UserLoggedOut { get; }

        ITogglDataSource GetDataSourceIfLoggedIn();

        IObservable<ITogglDataSource> LoginWithGoogle();
        IObservable<ITogglDataSource> Login(Email email, Password password);

        IObservable<ITogglDataSource> SignUpWithGoogle(bool termsAccepted, int countryId);
        IObservable<ITogglDataSource> SignUp(Email email, Password password, bool termsAccepted, int countryId);

        IObservable<Unit> Logout();

        IObservable<ITogglDataSource> RefreshToken(Password password);

        IObservable<string> ResetPassword(Email email);
    }
}
