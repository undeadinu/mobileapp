using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class SelectUserCalendarsViewModel : SelectUserCalendarsViewModelBase
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IRxActionFactory rxActionFactory;

        public UIAction Done { get; private set; }

        public SelectUserCalendarsViewModel(
            IUserPreferences userPreferences,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IRxActionFactory rxActionFactory
            )
            : base(userPreferences, interactorFactory, rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.navigationService = navigationService;
            this.rxActionFactory = rxActionFactory;
        }

        public override Task Initialize()
        {
            var enabledObservable = ForceItemSelection
                ? SelectCalendar.Elements
                    .Select(_ => SelectedCalendarIds.Any())
                    .DistinctUntilChanged()
                : Observable.Return(true);

            Done = rxActionFactory.FromAsync(done, enabledObservable);

            return base.Initialize();
        }

        private Task done()
            => navigationService.Close(this, SelectedCalendarIds.ToArray());
    }
}
