using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IStateMachineOwner
{

}
public class StateMachine
{
    private IStateMachineOwner owner;
    private Dictionary<Type, StateBase> stateDic = new Dictionary<Type, StateBase>();
    private readonly HashSet<StateBase> externalStates = new HashSet<StateBase>();
    public Type CurrentStateType { get => currentState?.GetType(); }
    public StateBase CurrentState => currentState;
    public bool HasState { get => currentState != null; }
    public bool IsRunning { get; private set; }
    private StateBase currentState;
    private StateBase subscribedState;
    private MonoManager listenerHost;
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="owner"></param>
    public void Init(IStateMachineOwner owner)
    {
        this.owner = owner;
    }
    /// <summary>
    /// 切换状态
    /// </summary>
    /// <typeparam name="T">具体要切换的状态类型</typeparam>
    /// <param name="reCurrstate">如果状态没变，是否需要刷新状态</param>
    /// <returns></returns>
    public bool ChangeState<T>(bool reCurrstate = false) where T : StateBase, new()
    {
        return ChangeState(GetState<T>(), reCurrstate);
    }
    public bool ChangeState(StateBase nextState, bool reCurrstate = false)
    {
        if (nextState == null || (currentState == nextState && !reCurrstate)) return false;
        if (!stateDic.ContainsValue(nextState) && externalStates.Add(nextState)) nextState.Init(owner);
        //退出当前状态
        if (currentState != null)
        {
            RemoveListeners(currentState);
            currentState.Exit();
        }
        //进入新状态
        currentState = nextState;
        currentState.Enter();
        AddListeners(currentState);
        IsRunning = true;
        return true;
    }
    private StateBase GetState<T>() where T : StateBase, new()
    {
        Type type = typeof(T);
        //如果字典中不存在
        if (!stateDic.TryGetValue(type, out StateBase state))
        {
            state = new T();
            state.Init(owner);
            stateDic.Add(type, state);
        }
        return state;
    }
    /// <summary>
    /// 停止工作，释放资源
    /// </summary>
    public void Stop(bool invokeExit = true)
    {
        if (currentState == null && stateDic.Count == 0 && externalStates.Count == 0)
        {
            return;
        }

        RemoveListeners(currentState);
        if (invokeExit)
        {
            currentState.Exit();
        }

        currentState = null;

        foreach (var item in stateDic.Values)
        {
            item.Unint();
        }
        foreach (var item in externalStates) item.Unint();
        externalStates.Clear();
        stateDic.Clear();
        IsRunning = false;
    }

    public void Pause()
    {
        if (!IsRunning || currentState == null)
        {
            return;
        }

        RemoveListeners(currentState);
        IsRunning = false;
    }

    public void Resume()
    {
        if (IsRunning || currentState == null)
        {
            return;
        }

        AddListeners(currentState);
        IsRunning = true;
    }

    private void AddListeners(StateBase state)
    {
        if (state == null || MonoManager.Instance == null)
        {
            return;
        }

        if (subscribedState == state && listenerHost == MonoManager.Instance)
        {
            return;
        }

        if (subscribedState != null)
        {
            RemoveListeners(subscribedState);
        }

        listenerHost = MonoManager.Instance;
        subscribedState = state;
        listenerHost.AddUpdateListener(state.Update);
        listenerHost.AddLateUpdateListener(state.LateUpdate);
        listenerHost.AddFixedUpdateListener(state.FixedUpdate);
    }

    private void RemoveListeners(StateBase state)
    {
        if (state == null)
        {
            return;
        }

        MonoManager host = listenerHost != null ? listenerHost : MonoManager.Instance;
        if (host != null)
        {
            host.RemoveUpdateListener(state.Update);
            host.RemoveLateUpdateListener(state.LateUpdate);
            host.RemoveFixedUpdateListener(state.FixedUpdate);
        }

        if (subscribedState == state)
        {
            subscribedState = null;
            listenerHost = null;
        }
    }
}

