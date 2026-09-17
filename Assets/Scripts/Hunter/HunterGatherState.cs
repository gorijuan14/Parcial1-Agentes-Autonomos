using UnityEngine;

public class HunterGatherState : HunterState
{
    [Header("References")]
    private Agent agent;
    private Transform target;
    private Pursue pursue;
    private StateIndicator stateIndicator;


    [Header("Stats")]
    private float gatherDuration = 2f;
    private float gatherTimer;
    private float gatherRadius = 1f;
    private bool hasReachedTarget;

    public HunterGatherState(HunterFSM hunter, Transform target) : base(hunter)
    {
        agent = hunter.GetComponent<Agent>();
        pursue = hunter.GetComponent<Pursue>();
        this.target = target;
        stateIndicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        gatherTimer = 0f;
        hasReachedTarget = false;

        pursue.SetTarget(target);
        agent.SetBehaviour(pursue);

        stateIndicator.SetHunterGather();
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

        if (!hasReachedTarget)
        {
            if (distance <= gatherRadius)
            {
                hasReachedTarget = true;

                agent.StopMovement();
                agent.SetBehaviour(null);

                Boid boid = target.GetComponent<Boid>();

                if (boid != null)
                {
                    boid.Collect();
                }
            }

            return;
        }

        gatherTimer += Time.deltaTime;

        agent.StopMovement();

        if (gatherTimer >= gatherDuration)
        {
            Boid boid = target.GetComponent<Boid>();

            if (boid != null)
            {
                boid.Disappear();
            }

            hunter.ChangeState(
                new HunterPatrolState(hunter)
            );
        }
    }

    public override void Exit()
    {
    }
}