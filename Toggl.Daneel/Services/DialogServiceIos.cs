using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.Services
{
    public sealed class DialogServiceIos : IDialogService
    {
        private readonly ITopViewControllerProvider topViewControllerProvider;

        public DialogServiceIos(ITopViewControllerProvider topViewControllerProvider)
        {
            Ensure.Argument.IsNotNull(topViewControllerProvider, nameof(topViewControllerProvider));

            this.topViewControllerProvider = topViewControllerProvider;
        }

        public IObservable<bool> Confirm(
            string title,
            string message,
            string confirmButtonText,
            string dismissButtonText)
        {
            return Observable.Create<bool>(observer =>
            {
                var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
                var confirm = UIAlertAction.Create(confirmButtonText, UIAlertActionStyle.Default, _ =>
                {
                    observer.OnNext(true);
                    observer.OnCompleted();
                });

                var dismiss = UIAlertAction.Create(dismissButtonText, UIAlertActionStyle.Cancel, _ =>
                {
                    observer.OnNext(false);
                    observer.OnCompleted();
                });

                alert.AddAction(confirm);
                alert.AddAction(dismiss);
                alert.PreferredAction = confirm;

                topViewControllerProvider
                    .TopViewController
                    .PresentViewController(alert, true, null);

                return Disposable.Empty;
            });
        }

        public IObservable<bool> ConfirmDestructiveAction(ActionType type)
        {
            return Observable.Create<bool>(observer =>
            {
                var actionSheet = UIAlertController.Create(
                    title: null,
                    message: null,
                    preferredStyle: UIAlertControllerStyle.ActionSheet
                );

                var (confirmText, cancelText) = selectTextByType(type);

                var cancelAction = UIAlertAction.Create(cancelText, UIAlertActionStyle.Cancel, _ =>
                {
                    observer.OnNext(false);
                    observer.OnCompleted();
                });

                var confirmAction = UIAlertAction.Create(confirmText, UIAlertActionStyle.Destructive, _ =>
                {
                    observer.OnNext(true);
                    observer.OnCompleted();
                });

                actionSheet.AddAction(cancelAction);
                actionSheet.AddAction(confirmAction);

                topViewControllerProvider
                    .TopViewController
                    .PresentViewController(actionSheet, true, null);

                return Disposable.Empty;
            });
        }

        public IObservable<Unit> Alert(string title, string message, string buttonTitle)
        {
            return Observable.Create<Unit>(observer =>
            {
                var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
                var alertAction = UIAlertAction.Create(buttonTitle, UIAlertActionStyle.Default, _ =>
                {
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                });

                alert.AddAction(alertAction);

                topViewControllerProvider
                    .TopViewController
                    .PresentViewController(alert, true, null);

                return Disposable.Empty;
            });
        }

        public IObservable<T> Select<T>(string title, IEnumerable<(string ItemName, T Item)> options, int initialSelectionIndex)
        {
            return Observable.Create<T>(observer =>
            {
                var actionSheet = UIAlertController.Create(
                    title,
                    message: null,
                    preferredStyle : UIAlertControllerStyle.ActionSheet);

                foreach (var (itemName, item) in options)
                {
                    var action = UIAlertAction.Create(itemName, UIAlertActionStyle.Default, _ =>
                    {
                        observer.OnNext(item);
                        observer.OnCompleted();
                    });

                    actionSheet.AddAction(action);
                }

                var cancelAction = UIAlertAction.Create(Resources.Cancel, UIAlertActionStyle.Cancel, _ =>
                {
                    observer.OnNext(default(T));
                    observer.OnCompleted();
                });

                actionSheet.AddAction(cancelAction);

                topViewControllerProvider
                    .TopViewController
                    .PresentViewController(actionSheet, true, null);

                return Disposable.Empty;
            });
        }

        private (string, string) selectTextByType(ActionType type)
        {
            switch (type)
            {
                case ActionType.DiscardNewTimeEntry:
                    return (Resources.Discard, Resources.Cancel);
                case ActionType.DiscardEditingChanges:
                    return (Resources.Discard, Resources.ContinueEditing);
                case ActionType.DeleteExistingTimeEntry:
                    return (Resources.Delete, Resources.Cancel);
                case ActionType.DiscardFeedback:
                    return (Resources.Discard, Resources.ContinueEditing);
            }

            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
