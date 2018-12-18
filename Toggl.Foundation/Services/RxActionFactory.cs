using System;
using System.Reactive;
using System.Threading.Tasks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Services
{
    public sealed class RxActionFactory : IRxActionFactory
    {
        private readonly ISchedulerProvider schedulerProvider;

        public RxActionFactory(ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.schedulerProvider = schedulerProvider;
        }

        public UIAction FromAction(Action action, IObservable<bool> enabledIf = null)
        {
            return UIAction.FromAction(action, schedulerProvider.MainScheduler, enabledIf);
        }

        public UIAction FromAsync(Func<Task> asyncAction, IObservable<bool> enabledIf = null)
        {
            return UIAction.FromAsync(asyncAction, schedulerProvider.MainScheduler, enabledIf);
        }

        public UIAction FromObservable(Func<IObservable<Unit>> workFactory, IObservable<bool> enabledIf = null)
        {
            return UIAction.FromObservable(workFactory, schedulerProvider.MainScheduler, enabledIf);
        }

        public InputAction<TInput> FromAction<TInput>(Action<TInput> action)
        {
            return InputAction<TInput>.FromAction(action, schedulerProvider.MainScheduler);
        }

        public InputAction<TInput> FromAsync<TInput>(Func<TInput, Task> asyncAction, IObservable<bool> enabledIf = null)
        {
            return InputAction<TInput>.FromAsync(asyncAction, schedulerProvider.MainScheduler, enabledIf);
        }

        public InputAction<TInput> FromObservable<TInput>(Func<TInput, IObservable<Unit>> workFactory, IObservable<bool> enabledIf = null)
        {
            return InputAction<TInput>.FromObservable(workFactory, schedulerProvider.MainScheduler, enabledIf);
        }
    }
}
