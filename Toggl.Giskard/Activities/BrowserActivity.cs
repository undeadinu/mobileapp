using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Webkit;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using static Android.Support.V7.Widget.Toolbar;
using static Toggl.Foundation.Resources;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed class BrowserActivity : MvxAppCompatActivity<BrowserViewModel>
    {
        private Toolbar toolbar;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.BrowserActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            setupToolbar();
            setupBrowser();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        private void setupToolbar()
        {
            toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
            toolbar.Title = Loading;

            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            toolbar.NavigationClick += onNavigateBack;
        }

        private void setupBrowser()
        {
            var webView = FindViewById<WebView>(Resource.Id.BrowserWebView);
            webView.SetWebViewClient(new TogglWebViewClient(onPageFinishedLoading));
            webView.SetWebChromeClient(new WebChromeClient());
            webView.Settings.JavaScriptEnabled = true;
            webView.LoadUrl(ViewModel.Url);
        }

        private void onPageFinishedLoading()
        {
            toolbar.Title = ViewModel.Title;
        }

        private void onNavigateBack(object sender, NavigationClickEventArgs e)
        {
            ViewModel.Close.Execute();
        }

        private class TogglWebViewClient : WebViewClient
        {
            private readonly Action pageFinishedLoadingCallback;

            public TogglWebViewClient(Action pageFinishedLoadingCallback)
            {
                Ensure.Argument.IsNotNull(pageFinishedLoadingCallback, nameof(pageFinishedLoadingCallback));

                this.pageFinishedLoadingCallback = pageFinishedLoadingCallback;
            }

            public override void OnPageFinished(WebView view, string url)
            {
                pageFinishedLoadingCallback();
            }
        }
    }
}
