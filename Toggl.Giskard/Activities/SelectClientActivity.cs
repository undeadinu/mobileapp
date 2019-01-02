using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.BlueStatusBar",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public partial class SelectClientActivity : ReactiveActivity<SelectClientViewModel>
    {
        private SelectClientRecyclerAdapter selectClientRecyclerAdapter = new SelectClientRecyclerAdapter();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SelectClientActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            setupLayoutManager(selectClientRecyclerAdapter);

            ViewModel.Clients
                .Subscribe(replaceClients)
                .DisposedBy(DisposeBag);

            backImageView.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            filterEditText.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            selectClientRecyclerAdapter.ItemTapObservable
                .Subscribe(ViewModel.SelectClient.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void setupLayoutManager(SelectClientRecyclerAdapter adapter)
        {
            var layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            selectClientRecyclerView.SetLayoutManager(layoutManager);
            selectClientRecyclerView.SetAdapter(adapter);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        private void replaceClients(IEnumerable<SelectableClientBaseViewModel> clients)
        {
            selectClientRecyclerAdapter.Items = clients.ToList();
        }
    }
}
