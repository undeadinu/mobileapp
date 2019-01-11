using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Toggl.Multivac.Extensions
{
    public static class RandomExtensions
    {
        static int seed = Environment.TickCount;

        static readonly ThreadLocal<Random> random = 
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        public static T RandomElement<T>(this IList<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection.Count == 0)
                throw new InvalidOperationException("Sequence contains no elements");

            return collection[random.Value.Next(collection.Count)];
        }
    }
}
