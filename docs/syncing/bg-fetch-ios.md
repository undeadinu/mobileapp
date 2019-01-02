Background Fetch on iOS
=======================

To enable background syncing we use the iOS [Background Fetch API](https://developer.apple.com/documentation/uikit/core_app/managing_your_app_s_life_cycle/preparing_your_app_to_run_in_the_background/updating_your_app_with_background_app_refresh). In our `BaseBackgroundSyncService` we subscribe to Login and Logout events, while in its subclass`BackgroundSyncServiceIos` we have the event callbacks.  In them we enable or disable fetching using the `SetMinimumBackgroundFetchInterval` method of `UIApplication`. We either set the interval to `MinimumBackgroundFetchInterval` (defined in `BaseBackgroundSyncService`), or to the constant `UIApplication.BackgroundFetchIntervalNever` to turn fetching on and off respectively. Bear in mind that this is the minimal interval between syncs, the system ultimately decides when sync will take place. This means that there's still the chance that a user's data won't be up-to-date when they open the app. Still, this works just fine for entities that don't change that often, like projects and workspaces. For time entries we will have to use push notifications once backend supports them.

What happens then is that whenever the app is in the background and we have fetching enabled, iOS will try and find some time to wake our app and call its `PerformFetch` handler. The handler supplies us with a `UIApplication` reference and a `Action<UIBackgroundFetchResult>` completion handler callback. We have to call this handler with the appropriate `UIBackgroundFetchResult` when our background fetch is done. 

Depending on the time it takes us to call the handler and its results, iOS can change the frequency with which it wakes our app up. If we take too long — we'll get called less often; if we return `NoData` too often — we'll get called less. If we return `Failed`, then iOS might either try making the request again, or, again, give us less calls — the docs are extremely opaque about this.

---

Previous topic: [Background sync](bg-sync.md)
Next topic: [Background Fetch on Android](bg-fetch-android.md)
Go to the [Index](index.md)