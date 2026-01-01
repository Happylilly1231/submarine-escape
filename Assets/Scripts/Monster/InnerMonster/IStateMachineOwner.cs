public interface IStateMachineOwner<T>
{
    void ChangeState(IState<T> newState);
}
