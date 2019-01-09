Sync state
==========

The sync states are classes which implement the `ISyncState` or the `ISyncState<T>` interface. The generic parameter of the latter is the type of input the state receives in the `Start` method.

The state's responsibility is to create an observable which should start executing once an observer subscribes to it. The observable should emit exactly once and it should emit a transition containing some result.

Naming conventions
------------------

There are two naming conventions:

1. The name of the state should start with a verb. Some examples from our codebase are:
    - `DeleteOldTimeEntriesState`
    - `TrackInaccessibleDataAfterCleanUpState`
    - `DetectLosingAccessToWorkspacesState`
    - `PersistNewWorkspacesState`
    - `CreateEntityState`
    - `MarkEntityAsUnsyncableState`
2. If the state has a result which has no parameter or if the parameter is the same as the input of the state it should be called `Done` unless there is a good reason not to. The name of the result should also refer to what happened in the state and not to what needs to be done with the result.

Example
-------

Below you will find an example of a state which would make an API request and return a different result based on the response.

```csharp

public sealed class ServerStatus : ISyncState<Exception>
{
    private readonly IApi api;

    private IStateResult ServerIsUp { get; } = new StateResult();

    private IStateResult<Exception> ServerIsDown { get; } = new StateResult();

    public ServerStatus(IApi api)
    {
        this.api = api;
    }

    public IObservable<ITransition> Start(Exception previousError = null)
    {
        var delay = previousError == null ? TimeSpan.FromSeconds(10) : TimeSpan.Zero;
        return api.CheckServerStatus() // returns IObservable<Unit>
            .Delay(delay)
            .Select(_ => ServerIsUp.Transition())
            .Catch((Exception exception) => Observable.Return(ServerIsDown.Transition(exception)));
    }
}

```

We could configure this state to loop if it fails:

```csharp
var entryPoint = new StateResult();
var otherState = new SomeState(api, database);
var serverStatus = new ServerStatus(api);
var transitions = new TransitionHandlerProvider();

transitions.ConfigureTransition(entryPoint, otherState);
transitions.ConfigureTransition(otherState.FailureResult, serverStatus);

transitions.ConfigureTransition(serverStatus.ServerIsUp, otherState);
transitions.ConfigureTransition(serverStatus.ServerIsDown, serverStatus);

// ...

stateMachine.Start(entryPoint.Transition());
```

---

Previous topic: [Transitions configuration](transitions-configuration.md)
Next topic: [Syncing tests](tests.md)
Go to the [Index](index.md)
