using System;
using System.Collections.Generic;
using System.Reactive;

namespace Toggl.Foundation.MvvmCross.Services
{
    public interface IDialogService
    {
        IObservable<bool> Confirm(
            string title,
            string message,
            string confirmButtonText,
            string dismissButtonText);

        IObservable<Unit> Alert(string title, string message, string buttonTitle);

        IObservable<bool> ConfirmDestructiveAction(ActionType type);

        IObservable<T> Select<T>(string title, IEnumerable<(string ItemName, T Item)> options, int initialSelectionIndex);
    }

    public enum ActionType
    {
        DiscardNewTimeEntry,
        DiscardEditingChanges,
        DeleteExistingTimeEntry,
        DiscardFeedback
    }
}
