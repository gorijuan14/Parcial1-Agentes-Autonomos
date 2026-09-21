using UnityEngine;

public abstract class HunterState
{
    protected HunterFSM hunterFSM;
    protected Hunter hunter;

    protected HunterState(HunterFSM hunterFSM, Hunter hunter)
    {
        this.hunterFSM = hunterFSM;
        this.hunter = hunter;
    }

    public abstract void Enter();

    public abstract void Update();

    public abstract void Exit();
}