using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Services;
using Toggl.PrimeRadiant.Settings;
using Xunit;
using static Toggl.Multivac.Extensions.EnumerableExtensions;

namespace Toggl.PrimeRadiant.Tests.Settings
{
    public sealed class SettingsStorageTests
    {
        public abstract class SettingsStorageTest
        {
            protected const string VersionString = "1.0";

            protected IKeyValueStorage Storage { get; } = new InMemoryKeyValueStorage();

            protected SettingsStorage SettingsStorage { get; }

            public SettingsStorageTest()
            {
                SettingsStorage = new SettingsStorage(Version.Parse(VersionString), Storage);
            }

            private sealed class InMemoryKeyValueStorage : KeyValueStorage
            {
                private readonly Dictionary<string, bool> bools = new Dictionary<string, bool>();
                private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
                private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
                private readonly Dictionary<string, long> longs = new Dictionary<string, long>();

                public override bool GetBool(string key)
                {
                    bools.TryGetValue(key, out var value);
                    return value;
                }

                public override string GetString(string key)
                {
                    strings.TryGetValue(key, out var value);
                    return value;
                }

                public override void SetBool(string key, bool value)
                {
                    bools[key] = value;
                }

                public override void SetString(string key, string value)
                {
                    strings[key] = value;
                }

                public override void SetInt(string key, int value)
                {
                    ints[key] = value;
                }

                public override void SetLong(string key, long value)
                {
                    longs[key] = value;
                }

                public override int GetInt(string key, int defaultValue)
                {
                    return ints.TryGetValue(key, out var value) ? value : defaultValue;
                }

                public override long GetLong(string key, long defaultValue)
                {
                    return longs.TryGetValue(key, out var value) ? value : defaultValue;
                }

                public override void Remove(string key)
                {
                    if (bools.Remove(key))
                    {
                        return;
                    }

                    if (strings.Remove(key))
                    {
                        return;
                    }

                    if (ints.Remove(key))
                    {
                        return;
                    }

                    longs.Remove(key);
                }

                public override void RemoveAllWithPrefix(string prefix)
                {
                    var keys = bools.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal));
                    foreach (var key in keys)
                        bools.Remove(key);

                    keys = strings.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal));
                    foreach (var key in keys)
                        strings.Remove(key);
                }
            }
        }

        public abstract class DeprecationCheckTest : SettingsStorageTest
        {
            internal abstract bool CallOutdatedMethod();

            protected abstract string Key { get; }

            [Fact, LogIfTooSlow]
            public void ReturnsTrueIfTheStoredVersionIsEqualToTheVersionPassedDuringConstruction()
            {
                Storage.SetString(Key, VersionString);

                var isOutdated = CallOutdatedMethod();

                isOutdated.Should().BeTrue();
            }

            [Theory, LogIfTooSlow]
            [InlineData("0.1")]
            [InlineData("0.2")]
            [InlineData("0.7.5")]
            [InlineData("0.9.7")]
            [InlineData("0.11.9")]
            public void ReturnsFalseIfTheStoredVersionIsSmallerThanTheVersionPassedDuringConstruction(string version)
            {
                Storage.SetString(Key, version);

                var isOutdated = CallOutdatedMethod();

                isOutdated.Should().BeFalse();
            }
        }

        public sealed class TheIsClientOutdatedMethod : DeprecationCheckTest
        {
            protected override string Key => "OutdatedClient";

            internal override bool CallOutdatedMethod()
                => SettingsStorage.IsClientOutdated();
        }

        public sealed class TheIsApiOutdatedMethod : DeprecationCheckTest
        {
            protected override string Key => "OutdatedApi";

            internal override bool CallOutdatedMethod()
                => SettingsStorage.IsApiOutdated();
        }

        public sealed class TheIsUnauthorizedMethod : SettingsStorageTest
        {
            [Property]
            public void ReturnsTrueIfTheApiTokenPassedIsTheSameAsTheOneStored(NonEmptyString key)
            {
                var apiToken = key.Get;
                SettingsStorage.SetUnauthorizedAccess(apiToken);

                var isUnauthorized = SettingsStorage.IsUnauthorized(apiToken);

                isUnauthorized.Should().BeTrue();
            }
            [Property]
            public void ReturnsFalseIfTheApiTokenPassedIsDifferentThanTheOneStored(NonEmptyString key)
            {
                var apiToken = key.Get;
                SettingsStorage.SetUnauthorizedAccess($"{apiToken}{apiToken}");

                var isUnauthorized = SettingsStorage.IsUnauthorized(apiToken);

                isUnauthorized.Should().BeFalse();
            }
        }

        public sealed class TheSetLastOpenedMethod : SettingsStorageTest
        {
            [Property]
            public void SetsTheStorageParameterToTheSerializedValueOfTheDatePassed(DateTimeOffset date)
            {
                SettingsStorage.SetLastOpened(date);

                SettingsStorage.GetLastOpened().Should().Be(date);
            }
        }

        public sealed class TheEnableCalendarsMethod : SettingsStorageTest
        {
            private SettingsStorage settingsStorage;
            IKeyValueStorage keyValueStorage;

            public TheEnableCalendarsMethod()
            {
                keyValueStorage = Substitute.For<IKeyValueStorage>();
                settingsStorage = new SettingsStorage(Version.Parse(VersionString), keyValueStorage);
            }

            [Theory]
            [InlineData("id1", "id2", "id3", "id4", "id5", "id1;id2;id3;id4;id5")]
            [InlineData("this-is-what-the-actual-id-looks-like", "another-one", "this-is-what-the-actual-id-looks-like;another-one")]
            public void StoresIdsAsOneStringUsingSemicolon(params string[] arguments)
            {
                var expectedStringToBeStored = arguments.Last();
                var ids = arguments.Take(arguments.Length - 1).ToArray();

                settingsStorage.SetEnabledCalendars(ids);

                keyValueStorage.Received().SetString("EnabledCalendars", expectedStringToBeStored);
            }

            [Fact, LogIfTooSlow]
            public void RemovesAllIdsIfNullIsPassed()
            {
                settingsStorage.SetEnabledCalendars(null);

                keyValueStorage.Received().Remove("EnabledCalendars");
            }

            [Fact, LogIfTooSlow]
            public void RemovesAllIdsIfEmptyArrayIsPassed()
            {
                settingsStorage.SetEnabledCalendars(new string[0]);

                keyValueStorage.Received().Remove("EnabledCalendars");
            }

            [Theory]
            [InlineData("firstone;", "another one")]
            [InlineData("good", ";", "another one")]
            [InlineData("first", "second", "la;st")]
            public void ThrowsIfAnyOfTheIdsContainTheSeparatorCharacter(params string[] ids)
            {
                Action tryingToSaveSeparatorCharacter = () => settingsStorage.SetEnabledCalendars(ids);

                tryingToSaveSeparatorCharacter.Should().Throw<ArgumentException>();
            }
        }

        public sealed class TheEnabledCalendarIdsMethod : SettingsStorageTest
        {
            [Theory]
            [InlineData("id1", "id2", "id0", "id")]
            [InlineData("id1")]
            [InlineData]
            [InlineData(null)]
            public void ReturnsTheStoredCalendars(params string[] ids)
            {
                SettingsStorage.SetEnabledCalendars(ids);

                var returnedIds = SettingsStorage.EnabledCalendarIds();

                if (!ids?.Any() ?? true)
                {
                    returnedIds.Should().BeEmpty();
                }
                else
                {
                    returnedIds.Should().Contain(ids);
                    returnedIds.Should().HaveSameCount(ids);
                }
            }
        }

        public sealed class TheEnabledCalendarsProperty : SettingsStorageTest
        {
            [Property]
            public void EmitsNewIdsWheneverNewCalendarsAreEnabled(NonEmptyArray<NonEmptyString> strings)
            {
                var calendarIds = strings
                    .Get
                    .Select(s => s.Get)
                    .Select(s => s.Replace(';', '-'))
                    .ToArray();
                var observer = Substitute.For<IObserver<List<string>>>();
                SettingsStorage.EnabledCalendars.Subscribe(observer);

                SettingsStorage.SetEnabledCalendars(calendarIds);

                Received.InOrder(() =>
                {
                    observer.OnNext(Arg.Is<List<string>>(list => list.SequenceEqual(calendarIds)));
                });
            }

            [Fact, LogIfTooSlow]
            public void EmitsEmptyListWhenNullIsSet()
            {
                var observer = Substitute.For<IObserver<List<string>>>();
                SettingsStorage.EnabledCalendars.Subscribe(observer);

                SettingsStorage.SetEnabledCalendars(null);

                observer.Received().OnNext(Arg.Is<List<string>>(list => list.None()));
            }

            [Fact, LogIfTooSlow]
            public void EmitsEmptyListWhenEmptyListIsSet()
            {
                var observer = Substitute.For<IObserver<List<string>>>();
                SettingsStorage.EnabledCalendars.Subscribe(observer);

                SettingsStorage.SetEnabledCalendars(new string[0]);

                observer.Received().OnNext(Arg.Is<List<string>>(list => list.None()));
            }
        }

        public sealed class TheCalendarNotificationsEnabledProperty : SettingsStorageTest
        {
            [Fact, LogIfTooSlow]
            public async Task StartsWithFalse()
            {
                var firstValue = await SettingsStorage.CalendarNotificationsEnabled.FirstAsync();

                firstValue.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void EmitsTrueWhenTurnedOn()
            {
                var observer = Substitute.For<IObserver<bool>>();
                SettingsStorage.CalendarNotificationsEnabled.Subscribe(observer);

                SettingsStorage.SetCalendarNotificationsEnabled(true);

                Received.InOrder(() =>
                {
                    observer.OnNext(false);
                    observer.OnNext(true);
                });
            }

            [Fact, LogIfTooSlow]
            public void EmitsFalseWhenTurnedOff()
            {
                var observer = Substitute.For<IObserver<bool>>();
                SettingsStorage.CalendarNotificationsEnabled.Subscribe(observer);

                SettingsStorage.SetCalendarNotificationsEnabled(true);
                SettingsStorage.SetCalendarNotificationsEnabled(false);

                Received.InOrder(() =>
                {
                    observer.OnNext(false);
                    observer.OnNext(true);
                    observer.OnNext(false);
                });
            }

            [Fact, LogIfTooSlow]
            public void DoesNotEmitAnyAdditionalTrueValues()
            {
                var observer = Substitute.For<IObserver<bool>>();
                SettingsStorage.CalendarNotificationsEnabled.Subscribe(observer);

                SettingsStorage.SetCalendarNotificationsEnabled(true);
                SettingsStorage.SetCalendarNotificationsEnabled(true);
                SettingsStorage.SetCalendarNotificationsEnabled(true);
                SettingsStorage.SetCalendarNotificationsEnabled(true);

                Received.InOrder(() =>
                {
                    observer.OnNext(false);
                    observer.OnNext(true);
                });
            }

            [Fact, LogIfTooSlow]
            public void DoesNotEmitAnyAdditionalFalseValues()
            {
                var observer = Substitute.For<IObserver<bool>>();
                SettingsStorage.CalendarNotificationsEnabled.Subscribe(observer);

                SettingsStorage.SetCalendarNotificationsEnabled(false);
                SettingsStorage.SetCalendarNotificationsEnabled(false);
                SettingsStorage.SetCalendarNotificationsEnabled(false);

                Received.InOrder(() =>
                {
                    observer.OnNext(false);
                });
            }
        }

        public sealed class TheTimeSpanBeforeCalendarNotificationsProperty : SettingsStorageTest
        {
            [Fact, LogIfTooSlow]
            public async Task StartsWithTheDefaultValue()
            {
                var firstValue = await SettingsStorage.TimeSpanBeforeCalendarNotifications.FirstAsync();

                firstValue.Should().Be(TimeSpan.FromMinutes(10));
            }

            [Property(MaxTest = 1)]
            public void EmitsNewValueWheneverNewValueIsSet(TimeSpan timeSpan)
            {
                var observer = Substitute.For<IObserver<TimeSpan>>();
                SettingsStorage.TimeSpanBeforeCalendarNotifications.Subscribe(observer);
                SettingsStorage.SetTimeSpanBeforeCalendarNotifications(timeSpan);

                Received.InOrder(() =>
                {
                    observer.OnNext(TimeSpan.FromMinutes(10));
                    observer.OnNext(timeSpan);
                });
            }
        }

        public sealed class TheCalendarWorkingHoursProperties : SettingsStorageTest
        {
            [Fact, LogIfTooSlow]
            public async Task DefaultWorkingHoursAreNineToFive()
            {
                var start = await SettingsStorage.CalendarWorkingHoursStart.FirstAsync();
                var end = await SettingsStorage.CalendarWorkingHoursEnd.FirstAsync();

                start.Should().Be(9);
                end.Should().Be(17);
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(Hours))]
            public void EmitsNewValueWheneverNewStartValueIsSet(int hour)
            {
                var observer = Substitute.For<IObserver<int>>();
                SettingsStorage.CalendarWorkingHoursStart.Subscribe(observer);
                SettingsStorage.SetCalendarWorkingHoursStart(hour);

                Received.InOrder(() =>
                {
                    observer.OnNext(9);
                    if (hour != 9)
                        observer.OnNext(hour);
                });
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(Hours))]
            public void EmitsNewValueWheneverNewEndValueIsSet(int hour)
            {
                var observer = Substitute.For<IObserver<int>>();
                SettingsStorage.CalendarWorkingHoursEnd.Subscribe(observer);
                SettingsStorage.SetCalendarWorkingHoursEnd(hour);

                Received.InOrder(() =>
                {
                    observer.OnNext(17);
                    if (hour != 17)
                        observer.OnNext(hour);
                });
            }

            public static IEnumerable<object[]> Hours
                => Enumerable.Range(0, 24).Select(hour => new object[] { hour });
        }
    }
}
