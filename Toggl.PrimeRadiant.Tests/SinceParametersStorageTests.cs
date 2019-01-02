using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Realm;
using Toggl.PrimeRadiant.Tests.Realm;
using Xunit;

namespace Toggl.PrimeRadiant.Tests
{
    public sealed class SinceParametersStorageTests
    {
        private static readonly DateTimeOffset someDate = new DateTimeOffset(2018, 04, 30, 18, 52, 33, TimeSpan.FromHours(-2));

        public abstract class BaseSinceParametersStorageTest
        {
            protected GenericTestAdapter<IDatabaseSinceParameter> TestAdapter { get; }

            private readonly ISinceParameterRepository repository;

            protected BaseSinceParametersStorageTest()
            {
                TestAdapter = new GenericTestAdapter<IDatabaseSinceParameter>();
                repository = new SinceParameterStorage(TestAdapter);
            }

            protected DateTimeOffset? Get(Type type)
            {
                try
                {
                    var method = typeof(SinceParameterStorage).GetMethod(nameof(SinceParameterStorage.Get));
                    var generic = method.MakeGenericMethod(type);
                    return (DateTimeOffset?)generic.Invoke(repository, null);
                }
                catch (TargetInvocationException exception)
                {
                    throw exception.InnerException;
                }
            }

            protected void Set(Type type, DateTimeOffset? since)
            {
                try
                {
                    var method = typeof(SinceParameterStorage).GetMethod(nameof(SinceParameterStorage.Set));
                    var generic = method.MakeGenericMethod(type);
                    generic.Invoke(repository, new object[] { since });
                }
                catch (TargetInvocationException exception)
                {
                    throw exception.InnerException;
                }
            }
        }

        public sealed class TheGetMethod : BaseSinceParametersStorageTest
        {
            [Theory]
            [MemberData(nameof(ExamplesOfUnsupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void ThrowsForUnsupportedTypes(Type unsupported)
            {
                Action get = () => Get(unsupported);

                get.Should().Throw<ArgumentException>();
            }

            [Theory]
            [MemberData(nameof(SupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void DoesNotThrowForSupportedTypes(Type unsupported)
            {
                Action get = () => Get(unsupported);

                get.Should().NotThrow();
            }

            [Theory]
            [MemberData(nameof(SupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void RetursNullWhenThereIsNoRecordForTheGivenTypeInTheDatabase(Type supported)
            {
                var since = Get(supported);

                since.Should().BeNull();
            }

            [Theory]
            [MemberData(nameof(SupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void GetsTheStoredValueForAGivenType(Type supported)
            {
                Set(supported, someDate);

                var since = Get(supported);

                since.Should().Be(someDate);
            }

            [Theory]
            [MemberData(nameof(SupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void GetsTheStoredNullValueForAGivenType(Type supported)
            {
                Set(supported, null);

                var since = Get(supported);

                since.Should().BeNull();
            }
        }

        public sealed class TheSetMethod : BaseSinceParametersStorageTest
        {
            [Theory]
            [MemberData(nameof(ExamplesOfUnsupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void ThrowsForUnsupportedTypes(Type unsupported)
            {
                Action get = () => Set(unsupported, someDate);

                get.Should().Throw<ArgumentException>();
            }

            [Theory]
            [MemberData(nameof(SupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void DoesNotThrowForSupportedTypes(Type unsupported)
            {
                Action get = () => Set(unsupported, someDate);

                get.Should().NotThrow();
            }

            [Theory]
            [MemberData(nameof(SupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void CreatesARecordWhenThereWasNoRecordForThisTypeBefore(Type supported)
            {
                Set(supported, someDate);

                var allRecords = TestAdapter.GetAll().ToList();
                allRecords.Should().HaveCount(1);
                allRecords[0].Since.Should().Be(someDate);
            }

            [Theory]
            [MemberData(nameof(SupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void UpdatesARecordWhenThereWasARecordForThisTypeBefore(Type supported)
            {
                Set(supported, someDate);

                Set(supported, someDate.AddDays(1));

                var allRecords = TestAdapter.GetAll().ToList();
                allRecords[0].Since.Should().Be(someDate.AddDays(1));
            }

            [Theory]
            [MemberData(nameof(SupportedTypes), MemberType = typeof(SinceParametersStorageTests))]
            public void UpdatesARecordToNullWhenThereWasARecordForThisTypeBefore(Type supported)
            {
                Set(supported, someDate);

                Set(supported, null);

                var allRecords = TestAdapter.GetAll().ToList();
                allRecords[0].Since.Should().Be(null);
            }
        }

        public static IEnumerable<object[]> SupportedTypes
            => new[]
            {
                new[] { typeof(IWorkspace) },
                new[] { typeof(ITag) },
                new[] { typeof(ITask) },
                new[] { typeof(IClient) },
                new[] { typeof(ITimeEntry) },
                new[] { typeof(IProject) }
            };

        public static IEnumerable<object[]> ExamplesOfUnsupportedTypes
            => new[]
            {
                new[] { typeof(IUser) },
                new[] { typeof(IPreferences) },
                new[] { typeof(IWorkspaceFeatureCollection) }
            };
    }
}
