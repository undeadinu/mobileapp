using System;
using System.Reactive.Linq;
using CoreGraphics;
using Toggl.Foundation.MvvmCross.Reactive;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross.Reactive;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class UIScrollViewExtensions
    {
        public static IObservable<int> CurrentPage(this IReactive<UIScrollView> reactive)
            => Observable
                .FromEventPattern(e => reactive.Base.DecelerationEnded += e, e => reactive.Base.DecelerationEnded -= e)
                .Select(_ => (int) (reactive.Base.ContentOffset.X / reactive.Base.Frame.Width));

        public static Action<int> CurrentPageObserver(this IReactive<UIScrollView> reactive)
            => page =>
            {
                var scrollPoint = new CGPoint(reactive.Base.Frame.Size.Width * page, 0);
                reactive.Base.SetContentOffset(scrollPoint, false);
            };

        public static IObservable<Unit> DecelerationEnded(this IReactive<UIScrollView> reactive)
            => Observable
                .FromEventPattern(e => reactive.Base.DecelerationEnded += e, e => reactive.Base.DecelerationEnded -= e)
                .SelectUnit();
    }
}
