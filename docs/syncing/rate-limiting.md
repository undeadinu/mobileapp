Rate limiting
=============

We use an implementation of the [leaky bucket](https://en.wikipedia.org/wiki/Leaky_bucket) algorithm to avoid overloading the server and running into any 429 Too Many Requests HTTP errors.

We use two buckets with different settings:
1. a "minute bucket" which allows 60 requests over the time span of 60 seconds (60r/60s)
2. a "seconds bucket" which allows 3 requests per 1 second (3r/1s)

The first bucket makes sure we never send more than 60 requests in a window of one minute. The second bucket makes sure we never send all of the requests within a very short time span. Using just the minute bucket could lead to a situation when we send 20 requests at once and some of them will fail with a 429 Too Many Requests. Using just the seconds bucket could lead to a situation when we send 180 requests in one minute.

If we want to make `n` requests (at once), we use the buckets in the following way:
1. check if there are `n` slots in the minute bucket
    - if yes, continue sending the request
    - else wait for the time period returned by the leaky bucket and then repeat the whole sync process from the start (the whole pull or push operation)
2. check for the `n` requests _in parallel_ if there is a free slot in the seconds bucket
    - if yes, send the HTTP request
    - else wait for the time period given by the leaky bucket and then send again (repeat until a slot is given)

The delay caused by the minute bucket can be up to 40 seconds (it takes at least 20 seconds to send 60 requests). The waiting time caused by the seconds bucket can be in theory at most 20 seconds (if we tried to send 60 requests in parallel, at least one of them will wait for at least 20 seconds).

In practice in our app it would be very hard to reach these long delays especially for the seconds bucket (we send at most 3 requests in parallel and the delay for a request should never be more than 1 second). It could happen that the minutes bucket overflows when the user has too many unsynced entities in the app (this can happen if the device was offline for a few days/weeks). There is nothing we can do about this until backend supports batch uploads (other than the CSV imports which are not a good option for us now).

Pull sync
---------

During pull sync we make 9 GET requests at once. We first try to allocate 9 slots from the minute bucket, and if that succeeds, we send the requests in "waves" of 3 requests. For each of the requests we wait until the seconds bucket gives us a free slot.

Push sync
---------

During push sync we never make HTTP requests in parallel.

We always check the minute bucket and if that succeeds, we continue and we will send the PR as soon as a slot from the seconds bucket is gained by repeatedly asking for a slot. Since we don't send requests in parallel this time we shouldn't wait more than once to get a free slot.

When the minute bucket doesn't grant us a slot we transition to a state where we wait for the given time until a next slot should be available and then we repeat the whole push sync from the beginning (from workspaces) because there is a chance that user modified some data while we were waiting for the slot.


Where rate limiting doesn't help us
-----------------------------------

- we only use rate limiting in the syncing algorithm so login, sign up, and reports can sometimes fail with a 429 or syncing can fail with a 429 for example after the user worked with reports very heavily and then pulled to refresh (this seems like a very artificial case)
- when the user uses a different Toggl client for the same user account at the same time (rate limiting on the server counts the number of requests per API token)

---

Previous topic: [Tests](tests.md)
Next topic: [Background sync](bg-sync.md)
Go to the [Index](index.md)
