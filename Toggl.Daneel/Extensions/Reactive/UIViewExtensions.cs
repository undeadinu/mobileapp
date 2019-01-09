using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreGraphics;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Reactive;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class UIViewExtensions
    {
        public static IObservable<Unit> Tap(this IReactive<UIView> reactive)
            => Observable.Create<Unit>(observer =>
            {
                var gestureRecognizer = new UITapGestureRecognizer(() => observer.OnNext(Unit.Default));
                gestureRecognizer.ShouldRecognizeSimultaneously = (recognizer, otherRecognizer) => true;
                reactive.Base.AddGestureRecognizer(gestureRecognizer);

                return Disposable.Create(() => reactive.Base.RemoveGestureRecognizer(gestureRecognizer));
            });

        public static IObservable<Unit> LongPress(this IReactive<UIView> reactive)
           => Observable.Create<Unit>(observer =>
           {
               var gestureRecognizer = new UILongPressGestureRecognizer(() => observer.OnNext(Unit.Default));
               gestureRecognizer.ShouldRecognizeSimultaneously = (recognizer, otherRecognizer) => true;
               reactive.Base.AddGestureRecognizer(gestureRecognizer);

               return Disposable.Create(() => reactive.Base.RemoveGestureRecognizer(gestureRecognizer));
           });

        public static Action<bool> IsVisible(this IReactive<UIView> reactive)
            => isVisible => reactive.Base.Hidden = !isVisible;

        public static Action<bool> IsVisibleWithFade(this IReactive<UIView> reactive)
            => isVisible =>
            {
                var alpha = isVisible ? 1 : 0;
                AnimationExtensions.Animate(
                    Animation.Timings.EnterTiming,
                    Animation.Curves.EaseIn,
                    () =>
                    {
                        reactive.Base.Alpha = alpha;
                    });
            };

        public static Action<UIColor> TintColor(this IReactive<UIView> reactive)
            => color => reactive.Base.TintColor = color;

        public static Action<bool> AnimatedIsVisible(this IReactive<UIView> reactive)
            => isVisible =>
            {
                reactive.Base.Transform = CGAffineTransform.MakeTranslation(0, 20);

                AnimationExtensions.Animate(
                    Animation.Timings.EnterTiming,
                    Animation.Curves.SharpCurve,
                    () =>
                    {
                        reactive.Base.Hidden = !isVisible;
                        reactive.Base.Transform = CGAffineTransform.MakeTranslation(0, 0);
                    }
                );
            };

        public static Action<float> AnimatedAlpha(this IReactive<UIView> reactive)
            => alpha =>
            {
                AnimationExtensions.Animate(
                    Animation.Timings.EnterTiming,
                    Animation.Curves.SharpCurve,
                    () => reactive.Base.Alpha = alpha
                );
            };

        public static Action<UIColor> AnimatedBackgroundColor(this IReactive<UIView> reactive) =>
            color =>
            {
                AnimationExtensions.Animate(
                    Animation.Timings.EnterTiming,
                    Animation.Curves.SharpCurve,
                    () => reactive.Base.BackgroundColor = color
                );
            };

        public static IDisposable BindAction(this IReactive<UIView> reactive, UIAction action)
        {
            return Observable.Using(
                    () => action.Enabled.Subscribe(e => { reactive.Base.UserInteractionEnabled = e; }),
                    _ => reactive.Base.Rx().Tap()
                )
                .Subscribe(action.Inputs);
        }
    }
}
