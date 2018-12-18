using System;
using System.Reactive.Disposables;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Fragments
{
    [MvxDialogFragmentPresentation(AddToBackStack = true)]
    public sealed partial class TermsOfServiceFragment : MvxDialogFragment<TermsOfServiceViewModel>
    {
        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        public TermsOfServiceFragment() { }

        public TermsOfServiceFragment(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = this.BindingInflate(Resource.Layout.TermsOfServiceFragment, null);

            InitializeViews(view);
            bindViews();

            return view;
        }

        private void bindViews()
        {
            privacyPolicyTextView.Rx()
                .BindAction(ViewModel.ViewPrivacyPolicy)
                .DisposedBy(DisposeBag);

            termsOfServiceTextView.Rx()
                .BindAction(ViewModel.ViewTermsOfService)
                .DisposedBy(DisposeBag);

            acceptButton.Rx()
                .BindAction(ViewModel.Close, _ => true)
                .DisposedBy(DisposeBag);
        }

        public override void OnResume()
        {
            base.OnResume();

            Dialog.Window.SetDefaultDialogLayout(Activity, Context, heightDp: 350);
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            ViewModel.Close.Execute(false);
        }
    }
}
