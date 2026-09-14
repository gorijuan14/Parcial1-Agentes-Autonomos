using UnityEngine;

public class HunterAttackState : HunterState
{
    private Agent agent;
    private Pursue pursue;
    private HunterSensor sensor;

    [SerializeField] private Transform target;

    public HunterAttackState(HunterFSM hunter, Transform target) : base(hunter)
    {
        agent = hunter.GetComponent<Agent>();
        pursue = hunter.GetComponent<Pursue>();
        sensor = hunter.GetComponentInChildren<HunterSensor>();

        this.target = target;
    }

    public override void Enter()
    {
        pursue.SetTarget(target);
        agent.SetBehaviour(pursue);

        Debug.Log($"Hunter entra en Attack. Objetivo: {target.name}");
    }

    public override void Update()
    {
        if (target == null)
        {
            hunter.ChangeState(
                new HunterPatrolState(hunter)
            );

            return;
        }

        float distance = Vector3.Distance(
            hunter.transform.position,
            target.position
        );

        if (distance <= 2f)
        {
            hunter.ChangeState(
                new HunterGatherState(hunter, target)
            );

            return;
        }

        Transform boid = sensor.GetClosestBoid();

        if (boid == null)
        {
            hunter.ChangeState(
                new HunterPatrolState(hunter)
            );

            return;
        }

        if (boid != target)
        {
            target = boid;
            pursue.SetTarget(target);
        }
    }

    public override void Exit()
    {
    }
}