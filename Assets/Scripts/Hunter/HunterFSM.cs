using System;
using System.Collections.Generic;

public class HunterFSM
{
    private HunterState currentState;
    public Enum CurrentState { get; private set; }
    private Dictionary<Enum, HunterState> states = new Dictionary<Enum, HunterState>();

    public void RegisterState(Enum key, HunterState state)
    {
        states[key] = state;
    }

    public void ChangeState(Enum key)
    {
        if(!states.ContainsKey(key)) return;

        HunterState newState = states[key];

        if (newState == currentState) return;
            
        currentState?.Exit();
        currentState = newState;
        CurrentState = key;
        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}
