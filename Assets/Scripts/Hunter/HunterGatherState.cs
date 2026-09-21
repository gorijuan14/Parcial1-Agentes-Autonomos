using UnityEngine;

public class HunterGatherState : HunterState
{
    [Header("References")]
    private Agent agent;
    private Pursue pursue;
    private Hunter hunterAgent;
    private StateIndicator stateIndicator;

    [Header("Stats")]
    private float gatherDuration = 2f;
    private float gatherTimer;
    private float gatherRadius = 1f;
    private bool hasReachedTarget;

    public HunterGatherState(HunterFSM hunterFSM, Hunter hunter)
        : base(hunterFSM, hunter)
    {
        agent = hunter.GetComponent<Agent>();
        pursue = hunter.GetComponent<Pursue>();
        hunterAgent = hunter;
        stateIndicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        gatherTimer = 0f;
        hasReachedTarget = false;

        Transform target = hunterAgent.CurrentTarget;

        if (target == null)
        {
            hunterFSM.ChangeState(HunterStates.Patrol);
            return;
        }

        pursue.SetTarget(target);
        agent.SetBehaviour(pursue);

        stateIndicator.SetHunterGather();
    }

    public override void Update()
    {
        Transform target = hunterAgent.CurrentTarget;

        if (target == null)
        {
            hunterFSM.ChangeState(HunterStates.Patrol);
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

            hunterAgent.ClearTarget();

            hunterFSM.ChangeState(HunterStates.Patrol);
        }
    }

    public override void Exit()
    {
    }
}