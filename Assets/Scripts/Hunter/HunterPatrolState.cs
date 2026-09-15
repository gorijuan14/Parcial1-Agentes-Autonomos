using UnityEngine;

public class HunterPatrolState : HunterState
{
    private Patrol patrol;
    private HunterSensor sensor;

    public HunterPatrolState(HunterFSM hunter) : base(hunter)
    {
        patrol = hunter.GetComponent<Patrol>();
        sensor = hunter.GetComponentInChildren<HunterSensor>();
    }

    public override void Enter()
    {
        Debug.Log($"Hunter entra en Patrol.");
        hunter.GetComponent<Hunter>().SetBehaviour(patrol);
    }

    public override void Update()
    {
        Transform boid = sensor.GetClosestBoid();

        if (boid != null)
        {
            hunter.ChangeState(
                new HunterAttackState(hunter, boid)
            );
        }
    }

    public override void Exit()
    {
    }
}