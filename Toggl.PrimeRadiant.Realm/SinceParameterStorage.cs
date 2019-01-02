using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Realm.Models;

namespace Toggl.PrimeRadiant.Realm
{
    internal sealed class SinceParameterStorage : ISinceParameterRepository
    {
        private readonly IRealmAdapter<IDatabaseSinceParameter> realmAdapter;

        private readonly object storageAccess = new object();

        private static readonly Dictionary<Type, long> typesToIdsMapping
            = new Dictionary<Type, long>
            {
                [typeof(IClient)] = 0,
                [typeof(IProject)] = 1,
                [typeof(ITag)] = 2,
                [typeof(ITask)] = 3,
                [typeof(ITimeEntry)] = 4,
                [typeof(IWorkspace)] = 5
            };

        public SinceParameterStorage(IRealmAdapter<IDatabaseSinceParameter> realmAdapter)
        {
            Ensure.Argument.IsNotNull(realmAdapter, nameof(realmAdapter));

            this.realmAdapter = realmAdapter;
        }

        public DateTimeOffset? Get<T>()
            where T : ILastChangedDatable
        {
            var id = getId<T>();

            lock (storageAccess)
            {
                try
                {
                    var record = realmAdapter.Get(id);
                    return record.Since;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }

        public void Set<T>(DateTimeOffset? since)
        {
            var id = getId<T>();
            var record = new RealmSinceParameter
            {
                Id = id,
                Since = since
            };

            lock (storageAccess)
            {
                try
                {
                    realmAdapter.Update(id, record);
                }
                catch (InvalidOperationException)
                {
                    realmAdapter.Create(record);
                }
            }
        }

        public bool Supports<T>()
            => typesToIdsMapping.TryGetValue(typeof(T), out _);

        public void Reset()
            => typesToIdsMapping.Values.ForEach(delete);

        private static long getId<T>()
        {
            if (typesToIdsMapping.TryGetValue(typeof(T), out var id))
                return id;

            throw new ArgumentException($"Since parameters for the type {typeof(T).FullName} cannot be stored.");
        }

        private void delete(long id)
        {
            lock (storageAccess)
            {
                try
                {
                    realmAdapter.Delete(id);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
