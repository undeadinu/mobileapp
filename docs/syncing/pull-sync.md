Pull sync loop
==============

The pull sync loop is responsible for querying the server and merging the obtained entities into the local database.

Fetching in waves
-----------------

We send at most 3 parallel requests to prevent overloading the server with large bursts of requests. We need to make 9 HTTP requests to fetch all the necessary data from the server which means we send requests in "waves" of 3 requests:

1. workspaces, user, workspace features
2. preferences, tags, clients
3. projects, time entries, tasks

The whole wave doesn't have to be completed before we start pulling the next wave. It's OK to send one request of the next wave once one of the requests from the previous requests is finished.

The dependencies between the requests are based on the order in which the entities are meant to be processed by the sync states.

Limiting the queries with 'since' parameters
--------------------------------------------

To limit the amount of data downloaded from the server, we use the URL variants which include the `since` parameter. Backend accepts since dates approximately _3 months_ into the past - we don't use dates older than _two months_, just to be sure.

If a `since` date is not available in the database or it is outdated, we fetch all the entities and update our `since` date in the database.

We calculate the `since` date by selecting the latest `ILastChangeDatable.At` value among all the pulled entities  (_note: this might yield a since date which is older than two months if the user didn't edit any of the entities (e.g., projects) for a long period of time so we will ignore it (we don't use since dates older than two months, remember?) and fetch all of the entities even the next time - there is currently nothing we could do about it, using the device's current time is risky because it might be incorrect and we might skip fetching some data if the device was ahead of the server_).

The order of persisting entities
--------------------------------

To make sure that the all the dependencies are already persisted, we process them in a given order:

1. Workspaces
2. User _(depends on workspaces)_
3. Workspaces' features _(depends on workspaces)_
4. Preferences
5. Tags  _(depends on workspaces)_
6. Clients  _(depends on workspaces)_
7. Projects _(depends on workspaces and clients)_
8. Tasks _(depends on workspaces, projects and user)_
9. Time entries _(depends on workspaces, projects, tasks, tags, users)_

Conflict resolution
-------------------

We use conflict resolution and rivals resolution to avoid any data inconsistencies in our database - namely to prevent having two running time entries at one time in the database. There is a [dedicated chapter](conflict-resolution.md) which describes the conflict resolution algorithms.

Placeholders
------------

Time entries keep references to the projects even after the projects were _archived_ (and possibly in other scenarios where user loses access to a project), this means that we cannot rely on the projects being in the database even if we respect the order of persisting the entities by types.

To prevent the app from crashing and to increase the UX of the app, we create placeholders for the projects which are referenced by time entries but are not in the database instead of ignoring them or removing these references. We then later try to fetch the details of these projects using the reports API. This way we will be able to update our local data when user gains back the access to a project or if the project becomes active again.

Pruning old data
----------------

After all data is pulled and persisted, we remove unnecessary data as part of the pull-sync loop.

- We remove any _time entry_ which was started more than _two months_ ago.
- We remove any _project placeholder_ which is not referenced by any time entry in the database.

_Note: we might move the pruning to a separate loop in the future._

Retry loop
----------

We use [rate limiting](rate-limiting.md) to minimize the chance of overflowing the server's leaky bucket.

If our internal leaky bucket overflows (the minute bucket) and we think that sending more requests to the server would cause a `429 Too Many Requests` client error we stop sending requests and wait until we think we will have a free slot and then repeat try again.

Handling errors
---------------

When a `ServerErrorException` or a `ClientErrorException` is caught we stop the syncing process. The `ApiDeprecatedException`, `ClientDeprecatedException`, or `UnauthorizedException` errors need to be handled carefully because they have important implications for the user (either the user has to update the app or he needs to login again).

Where everything is implemented in the code
-------------------------------------------

The code is located (mostly) under the namespace `Toggl.Foundation.Sync.States.Pull`.

We initiate the HTTP requests in the state class `FetchAllSinceState`.

Individual states are instances of the `PersistSingletonState` (user, preferences) and `PersistListState` (the rest).

This basic logic is then wrapped in `SinceDateUpdatingPersistState` for the entities for which we store the `since` date. All states are wrapped with `ApiExceptionCatchingPersistState` which catches known exceptions and leads into the retry loop.

The logic of creating project placeholders is implemented in the `CreateArchivedProjectPlaceholdersState` and fetching the details of these projects using the reports API is done in `TryFetchInaccessibleProjectsState`.

The states are instantiated and connected in the `Toggl.Foundation.TogglSyncManagerFactory` class.

---

Previous topic: [Conflict resolution](conflict-resolution.md)
Next topic: [Push sync](push-sync.md)
Go to the [Index](index.md)
