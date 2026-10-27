using System;
using System.Collections.Generic;

public class HunterFSM
{
    private HunterState currentState;
    private readonly Hunter hunter;
    public HunterFSM(Hunter hunter = null)
    {
        this.hunter = hunter;
    }
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
        // Global interrupts belong to the FSM, not to a scene controller or a wolf.
        if (hunter != null)
        {
            if (hunter.IsCaptured)
                ChangeState(HunterStates.Dead);
            else if (!Equals(CurrentState, HunterStates.Dead) && !Equals(CurrentState, HunterStates.Flee) &&
                hunter.IsVisible && !hunter.IsInSafeZone && hunter.IsSpottedByVisibleWolf)
                ChangeState(HunterStates.Flee);
            else if (!Equals(CurrentState, HunterStates.Dead) && !Equals(CurrentState, HunterStates.Flee) &&
                !Equals(CurrentState, HunterStates.Cautious) && hunter.IsVisible)
            {
                var direction = hunter.IntendedDirection;
                if (hunter.FindBlockingUnawareWolf(direction) != null)
                {
                    hunter.CautionDirection = direction;
                    hunter.CautionDestination = hunter.IntendedDestination;
                    ChangeState(HunterStates.Cautious);
                }
            }
        }
        currentState?.Update();
    }

    public void Stop()
    {
        currentState?.Exit();
        currentState = null;
        CurrentState = null;
    }
}
