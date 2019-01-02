using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHolders.Country;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.WhiteStatusBarLightIcons",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public partial class SelectCountryActivity : ReactiveActivity<SelectCountryViewModel>
    {
        private SimpleAdapter<SelectableCountryViewModel> recyclerAdapter =
            new SimpleAdapter<SelectableCountryViewModel>(Resource.Layout.SelectCountryActivityCountryCell,
                CountrySelectionViewHolder.Create);

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SelectCountryActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            InitializeViews();

            setupRecyclerView(adapter: recyclerAdapter);

            ViewModel.Countries
                .Subscribe(replaceCountries)
                .DisposedBy(DisposeBag);

            backImageView.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            filterEditText.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            recyclerAdapter.ItemTapObservable
                .Subscribe(ViewModel.SelectCountry.Inputs)
                .DisposedBy(DisposeBag);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                ViewModel.Close.Execute();
                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        private void setupRecyclerView(SimpleAdapter<SelectableCountryViewModel> adapter)
        {
            var layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.SetAdapter(adapter);
        }

        private void replaceCountries(IEnumerable<SelectableCountryViewModel> countries)
        {
            recyclerAdapter.Items = countries.ToList();
        }
    }
}
