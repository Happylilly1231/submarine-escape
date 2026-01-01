using UnityEngine;

public class StateMachine<T>
{
    private IState<T> _currentState;
    private T _owner;

    public StateMachine(T owner)
    {
        _owner = owner;
    }

    public void ChangeState(IState<T> newState)
    {
        _currentState?.Exit(_owner);
        _currentState = newState;
        Debug.Log(_currentState);
        _currentState?.Enter(_owner);
    }

    public void Update()
    {
        _currentState?.Update(_owner);
    }

    // 현재 상태 종료 함수(-> 상태 머신을 2개 사용하기 때문에 다른 상태머신으로 ChangeState할 때 현재 상태머신을 그냥 종료하기 위해 필요)
    public void ExitState()
    {
        _currentState?.Exit(_owner);
        _currentState = null;
    }
}