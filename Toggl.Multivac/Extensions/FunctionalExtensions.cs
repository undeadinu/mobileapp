using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Multivac.Extensions
{
    public static class CommonFunctions
    {
        public static T Identity<T>(T x) => x;

        public static bool And(bool x, bool y) => x && y;

        public static bool Or(bool x, bool y) => x || y;

        public static bool Invert(bool x) => !x;

        public static bool NotNull(object obj) => obj != null;

        public static string ToString<T>(T obj) => obj.ToString();

        public static void DoNothing<T>(T x) { }

        public static T1 First<T1, T2>(T1 result, T2 _) => result;

        public static T2 Second<T1, T2>(T1 _, T2 result) => result;
    }

    public static class FunctionalExtensions
    {
        public static TResult Apply<T, TResult>(this T self, Func<T, TResult> funcToApply)
            => funcToApply(self);

        public static void ForEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            Ensure.Argument.IsNotNull(self, nameof(self));
            Ensure.Argument.IsNotNull(action, nameof(action));

            foreach (T item in self)
            {
                action(item);
            }
        }

        public static void ForEach<T1, T2>(this IEnumerable<ValueTuple<T1, T2>> self, Action<T1, T2> action)
        {
            Ensure.Argument.IsNotNull(self, nameof(self));
            Ensure.Argument.IsNotNull(action, nameof(action));

            foreach (var item in self)
            {
                action(item.Item1, item.Item2);
            }
        }

        public static IEnumerable<(T, int)> Indexed<T>(this IEnumerable<T> enumerable)
        {
            Ensure.Argument.IsNotNull(enumerable, nameof(enumerable));

            return enumerable.Select((x, i) => (x, i));
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> self, Action<T> action)
        {
            Ensure.Argument.IsNotNull(self, nameof(self));
            Ensure.Argument.IsNotNull(action, nameof(action));

            self.ForEach(action);
            return self;
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> self, Action action)
        {
            self.Do(_ => action());
            return self;
        }
    }
}
