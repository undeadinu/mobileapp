Background Sync
===============

As of now, we don't support real-time background sync using push notifications. Instead, we perform full sync at regular intervals using the `RunBackgroundSyncInteractor`. Further down the line we might simplify this sync in order to make it less taxing on the system.

In order to preserve battery life we only schedule syncs when the user is logged in. We ensure that by using `IBackgroundSyncService` and `BaseBackgroundSyncService`. This singleton subscribes to login and logout events emitted by the `LoginManager` and enables and disables syncing accordingly.

## How To Test Background Syncing

Background syncing is executed whenever the operating system decides that it is a good time for background fetch. We don't have any influence on how often this happens or if it happens at all. The only thing we know that if the conditions are good (the device is connected to the wifi and is charging), it should happen several times a day on both iOS and Android.

We track several analytics events to monitor the performance of background syncing:
- `BackgroundSyncStarted` is tracked whenever syncing in background starts.
- `BackgroundSyncFailed` is tracked whenever there is an unhandled error.
- `BackgroundSyncFinished` is tracked whenever syncing runs until the end. It can fail because of various reasons (e.g., a 429 or 5xx response from the server, the device goes offline in the middle of syncing, ...) or it can successfully run until the end. We don't distinguish these two events at the moment. It is important to know if the whole process finished on its own or if it was killed by the operating system because it took too long (e.g., iOS will kill the process after ca 30 seconds).

At least one day after a testing build is released to internal testers, we should check the numbers in AppCenter for the new release and see if there are any `BackgroundSyncStarted` and if their count is very close to `BackgroundSyncFinished` and that there is no `BackgroundSyncFailed`. Too many unfinished background syncs or any failures could indicate a bug in the syncing algorithm.

Also check the Firebase performance results for the `BackgroundSync` operation. It should not take significantly more time than the previous version. A noticable increase in execution time could indicate a bug in the syncing algorithm.

---

Previous topic: [Syncing tests](tests.md)
Next topic: [Background Fetch on iOS](bg-fetch-ios.md)
Go to the [Index](index.md)
