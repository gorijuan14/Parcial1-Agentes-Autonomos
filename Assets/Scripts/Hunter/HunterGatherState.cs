using UnityEngine;

public class HunterGatherState : HunterState
{
    private Agent agent;
    private Transform target;

    private float gatherDuration = 2f;
    private float gatherTimer;
    private float gatherRadius = 0.5f;

    private bool hasReachedTarget;

    public HunterGatherState(HunterFSM hunter, Transform target) : base(hunter)
    {
        agent = hunter.GetComponent<Agent>();
        this.target = target;
    }

    public override void Enter()
    {
        gatherTimer = 0f;
        hasReachedTarget = false;
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