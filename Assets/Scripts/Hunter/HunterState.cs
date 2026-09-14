using UnityEngine;

public abstract class HunterState
{
    protected HunterFSM hunter;

    public HunterState(HunterFSM hunter)
    {
        this.hunter = hunter;
    }

    public virtual void Enter()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void Exit()
    {
    }
}