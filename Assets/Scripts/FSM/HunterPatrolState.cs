using UnityEngine;

public class HunterPatrolState : HunterState
{
    private Patrol patrol;

    public HunterPatrolState(HunterFSM hunter) : base(hunter)
    {
        patrol = hunter.GetComponent<Patrol>();
    }

    public override void Enter()
    {
        hunter.GetComponent<SteeringAgent>().SetBehaviour(patrol);
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
    }
}