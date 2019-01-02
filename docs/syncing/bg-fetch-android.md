Background Fetch on Android
=======================

To enable background syncing we use Android's [`JobScheduler` API](https://developer.android.com/reference/android/app/job/JobScheduler). Similar to what happens on iOS, in our `BaseBackgroundSyncService` we subscribe to Login and Logout events, while in its subclass `BackgroundSyncServiceAndroid` we schedule or cancel the job, depending on the user's login state. Like on iOS, we request the OS for the syncs to happen once in every interval defined on `MinimumBackgroundFetchInterval`; whether the sync will run that often depends on many a variable like battery, connectivity among others. 

When creating the `JobService` we specific the following properties of the builder:

- `.SetRequiredNetworkType(NetworkType.Any)`: This means the service will run on both Wi-Fi and Cellular networks
- `.SetPeriodic(periodicity)`: This means the job will, optimistically, run once every `periodicity` milliseconds
- `.SetPersisted(true)`: This means the job will be reescheduled automatically once the system reboots. For this to work the app needs the `android.permission.RECEIVE_BOOT_COMPLETED` permission, so don't touch it!

Once Xamarin starts supporting androidx, we should consider moving to using [WorkManager](https://developer.android.com/topic/libraries/architecture/workmanager/) instead of `JobScheduler`, but that's more a of wish than a need.

---

Previous topic: [Background Fetch on iOS](bg-fetch-ios.md)
This is the end of the syncing docs - you can go back to the [index](index.md)
