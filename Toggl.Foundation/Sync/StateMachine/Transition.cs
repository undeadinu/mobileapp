namespace Toggl.Foundation.Sync
{
    public interface ITransition
    {
        IStateResult Result { get; }
    }

    internal sealed class Transition : ITransition
    {
        public IStateResult Result { get; }

        public Transition(StateResult result)
        {
            Result = result;
        }
    }

    internal sealed class Transition<T> : ITransition
    {
        public IStateResult Result { get; }
        public T Parameter { get; }

        public Transition(StateResult<T> result, T parameter)
        {
            Result = result;
            Parameter = parameter;
        }
    }
}
