public interface IState<T>
{
    void Enter(T owner);
    void Update(T owner);
    void FixedUpdate(T owner) { }
    void Exit(T owner);
}
