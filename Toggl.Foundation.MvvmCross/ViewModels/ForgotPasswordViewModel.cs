using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class ForgotPasswordViewModel : MvxViewModel<EmailParameter, EmailParameter>
    {
        private readonly ITimeService timeService;
        private readonly IUserAccessManager userAccessManager;
        private readonly IAnalyticsService analyticsService;
        private readonly IMvxNavigationService navigationService;
        private readonly IRxActionFactory rxActionFactory;

        private readonly TimeSpan delayAfterPassordReset = TimeSpan.FromSeconds(4);

        public BehaviorSubject<Email> Email { get; } = new BehaviorSubject<Email>(Multivac.Email.Empty);
        public IObservable<string> ErrorMessage { get; }
        public IObservable<bool> PasswordResetSuccessful { get; }

        public UIAction Reset { get; }
        public UIAction Close { get; }

        public ForgotPasswordViewModel(
            ITimeService timeService,
            IUserAccessManager userAccessManager,
            IAnalyticsService analyticsService,
            IMvxNavigationService navigationService,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.timeService = timeService;
            this.userAccessManager = userAccessManager;
            this.analyticsService = analyticsService;
            this.navigationService = navigationService;
            this.rxActionFactory = rxActionFactory;

            Reset = rxActionFactory.FromObservable(reset, Email.Select(email => email.IsValid));
            Close = rxActionFactory.FromAction(returnEmail, Reset.Executing.Invert());

            ErrorMessage = Reset.Errors
                .Select(toErrorString)
                .StartWith("");

            PasswordResetSuccessful = Reset.Elements
                .Select(_ => true)
                .StartWith(false);
        }

        public override void Prepare(EmailParameter parameter)
        {
            Email.OnNext(parameter.Email);
        }

        private IObservable<Unit> reset()
        {
            return userAccessManager.ResetPassword(Email.Value)
                .Track(analyticsService.ResetPassword)
                .SelectUnit()
                .Do(closeWithDelay);
        }

        private void closeWithDelay()
        {
            timeService.RunAfterDelay(delayAfterPassordReset, returnEmail);
        }

        private void returnEmail()
        {
            navigationService.Close(this, EmailParameter.With(Email.Value));
        }

        private string toErrorString(Exception exception)
        {
            switch (exception)
            {
                case BadRequestException _:
                    return Resources.PasswordResetEmailDoesNotExistError;

                case OfflineException _:
                    return Resources.PasswordResetOfflineError;

                case ApiException apiException:
                    return apiException.LocalizedApiErrorMessage;

                default:
                    return Resources.PasswordResetGeneralError;
            }
        }
    }
}
