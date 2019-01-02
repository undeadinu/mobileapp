# Syncing tests

## Sync Tests Project

There is a project called `Toggl.Foundation.Sync.Tests` which contains all the sync tests.

To run the tests simply run `sh build.sh --target Tests.Sync`.

Bitrise is configured to run sync tests once every day on the `develop` branch.

It is easy and convenient to debug these tests using Rider. Just make sure to change `{CAKE_COMMIT_HASH}` in the `UserAgent` setup with some alphanumeric string before you run them (otherwise you will get a runtime error).

## The Idea Of Syncing Tests

Syncing tests need to make API calls and store data in a database. We could mock the server logic and the database storage, but that might lead to inaccurate tests. We use the staging server and the database storage directly.

The syncing tests are quite simple:

1. The state of the server is defined and it is pushed to the server.
2. The state of the database is defined (it can be derived from the server state and use the IDs assigned by the server) and stored in Realm.
3. Any necessary syncing operations are performed (push, full sync, clean-up).
4. A fresh state of the server and the storage is obtained and assertions are executed on the state.

There are helpers for pushing and pulling state to/from the server and the local database.

For most tests it should be enough to derive a class form `BaseComplexSyncTest` and override the abstract methods.

## Current Limitations

- All the entities created on the server have the `At` value equal to the time when they were created. And they were all created at roughly the same time. Specifying a given time on the server is not possible.

---

Previous topic: [Sample sync state](example.md)
Next topic: [Rate limiting](rate-limiting.md)
Go to the [Index](index.md)

