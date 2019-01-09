Push sync loop
==============

Pushing entities to the server is more complicated than pulling data and it requires a lot more code and more syncing states.

We push entities in the same order we pull them - for the same reason - we need to have the dependencies of some entity on the server before we start pushing this entity to avoid errors.

The loop
--------

For every entity type we define a simple loop: take the oldest unsynced entity, push it to the server, repeat until there are no more unsynced entities, then proceed to the loop for the next entity type. This logic is implemented in the `PushState` class.

Pushing a single entity, then, means choosing one of the 4 operations described below:
- the entity was *created* on the device and has not been synced yet -> create entity _on the server_
- the entity was *deleted* on the device and *has not been synced yet* -> delete it _on the device_
- the entity was *deleted* on the device and it exists on the server -> delete it _on the server_
- the entity was *updated* on the device -> update it on server

_Note: we only support all of these operations for time entries, the other entities can be either only created in the app but not updated or deleted or only updated._

Each of the individual states simply create a HTTP request and if it is successful then the local entity is updated with the entity data in the body of the HTTP response (using the `ignoreIfChangedLocally` conflict resolution).

If the server reports an error, we try to resolve the situation according to the type of error:
- for server errors we stop the current syncing process
- for the client error `429 Too Many Requests` we also stop the current syncing process
- for other client errors we mark the entity as unsyncable and skip it until the user resolves the error in the app

If our internal leaky bucket overflows (the minute leaky bucket) we wait until the bucket has a free slot and then we continue pushing from the very start (from workspaces).

If pushing of a new entity fails and another entity depends on this entity (e.g., syncing of a new project fails and it is used by a new time entry, which should also be synced) then we don't propagate the "unsyncable" state any further, but we can be sure that syncing of that entity fails as well, because its dependency is not known to the server, and it will be marked as "unsyncable" as well.

_Note: this might be a problem, because currently the app does not allow editing and repeated syncing of projects and clients. User can't create tasks in the mobile app at the moment._

---

Previous topic: [Pull sync](pull-sync.md)
Next topic: [State machine](state-machine.md)
Go to the [Index](index.md)
