using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using MvvmCross;
using MvvmCross.Platforms.Android;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Services;
using Android.App;
using Android.OS;
using MvvmCross.Platforms.Android.Views;
using Toggl.Giskard.Views;
using Toggl.Multivac.Extensions;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Object = Java.Lang.Object;

namespace Toggl.Giskard.Services
{
    public sealed class DialogServiceAndroid : Object, IDialogService
    {
        private readonly long delayBeforeLookingForTopActivityAgain = 300;

        public IObservable<bool> Confirm(string title, string message, string confirmButtonText, string dismissButtonText)
        {
            return Observable.Create<bool>(observer =>
            {
                void showDialogIfActivityIsThere(Activity activity)
                {
                    if (activity == null || activity.IsFinishing)
                    {
                        observer.CompleteWith(false);
                        return;
                    }

                    var builder = new AlertDialog.Builder(activity, Resource.Style.TogglDialog).SetMessage(message)
                        .SetPositiveButton(confirmButtonText, (s, e) => observer.CompleteWith(true));

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        builder = builder.SetTitle(title);
                    }

                    if (!string.IsNullOrEmpty(dismissButtonText))
                    {
                        builder = builder.SetNegativeButton(dismissButtonText, (s, e) => observer.CompleteWith(false));
                    }

                    var dialog = builder.Create();
                    dialog.CancelEvent += (s, e) => observer.CompleteWith(false);

                    dialog.Show();
                }

                tryToRunOnTopActivity(showDialogIfActivityIsThere);

                return Disposable.Empty;
            });
        }

        private void tryToRunOnTopActivity(Action<Activity> action)
        {
            var runningTopActivity = findRunningTopActivity();

            runningTopActivity?.RunOnUiThread(() => action(runningTopActivity));

            if (runningTopActivity == null)
            {
                var handler = new Handler(MvxAndroidApplication.Instance.MainLooper);
                handler.PostDelayed(() => action(findRunningTopActivity()), delayBeforeLookingForTopActivityAgain);
            }
        }

        private Activity findRunningTopActivity()
        {
            var topActivity = Mvx.Resolve<IMvxAndroidCurrentTopActivity>() as QueryableMvxLifecycleMonitorCurrentTopActivity;
            return topActivity?.FindNonFinishingCurrentActivity();
        }

        public IObservable<T> Select<T>(string title, IEnumerable<(string ItemName, T Item)> options, int initialSelectionIndex = 0)
        {
            var activity = Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity;

            return Observable.Create<T>(observer =>
            {
                var dialog = new ListSelectionDialog<T>(activity, title, options, initialSelectionIndex, observer.CompleteWith);
                activity.RunOnUiThread(dialog.Show);
                return Disposable.Empty;
            });
        }

        public IObservable<Unit> Alert(string title, string message, string buttonTitle)
            => Confirm(title, message, buttonTitle, null).Select(_ => Unit.Default);

        public IObservable<bool> ConfirmDestructiveAction(ActionType type)
        {
            switch (type)
            {
                case ActionType.DiscardNewTimeEntry:
                    return Confirm(null, Resources.DiscardThisTimeEntry, Resources.Discard, Resources.Cancel);
                case ActionType.DiscardEditingChanges:
                    return Confirm(null, Resources.DiscardEditingChanges, Resources.Discard, Resources.ContinueEditing);
                case ActionType.DeleteExistingTimeEntry:
                    return Confirm(null, Resources.DeleteThisTimeEntry, Resources.Delete, Resources.Cancel);
                case ActionType.DiscardFeedback:
                    return Confirm(null, Resources.DiscardMessage, Resources.Discard, Resources.ContinueEditing);
            }

            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
