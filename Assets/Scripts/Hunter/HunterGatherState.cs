using UnityEngine;

public class HunterGatherState : HunterState
{
    private readonly HunterSensor sensor;
    private readonly Arrive arrive;
    private readonly StateIndicator indicator;
    private Boid claimedBoid;
    private float gatherTimer;
    private float nextRouteAttempt;
    private bool completed;
    private float approachTimer;
    private float stuckTimer;
    private Vector3 progressPosition;

    public HunterGatherState(HunterFSM fsm, Hunter hunter) : base(fsm, hunter)
    {
        sensor = hunter.GetComponentInChildren<HunterSensor>();
        arrive = hunter.GetComponent<Arrive>();
        indicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        claimedBoid = null;
        completed = false;
        gatherTimer = 0f;
        nextRouteAttempt = 0f;
        approachTimer = 0f;
        stuckTimer = 0f;
        progressPosition = hunter.transform.position;
        hunter.GatherStatus = "Acercandose";
        indicator.SetHunterGather();
    }

    public override void Update()
    {
        Transform target = hunter.CurrentTarget;
        Boid boid = target != null ? target.GetComponent<Boid>() : null;
        if (boid == null || !boid.IsDead || !boid.isActiveAndEnabled ||
            (claimedBoid == null && boid.IsCollected) ||
            (claimedBoid != null && (boid != claimedBoid || !boid.IsCollected)))
        {
            ReturnToSearch();
            return;
        }

        if (claimedBoid == null)
        {
            approachTimer += Time.deltaTime;
            if ((hunter.transform.position - progressPosition).sqrMagnitude >= 0.0625f)
            {
                progressPosition = hunter.transform.position;
                stuckTimer = 0f;
            }
            else
                stuckTimer += Time.deltaTime;

            if (stuckTimer >= hunter.GatherStuckTimeout || approachTimer >= hunter.GatherApproachTimeout)
            {
                // Do not repeatedly select the same inaccessible corpse on the next scan.
                sensor.IgnoreTemporarily(target, 10f);
                ReturnToSearch();
                return;
            }

            // Reorient toward the already detected corpse, including at the end of an A* route.
            hunter.FaceTarget(target);
            if (sensor.CanSee(target) && hunter.CanMoveDirectlyTo(target.position))
            {
                StopNavigation();
                Vector3 offset = target.position - hunter.transform.position;
                offset.y = 0f;
                hunter.GatherStatus = $"Acercandose: {offset.magnitude:0.0} m";
                if (offset.magnitude <= hunter.GatherRange)
                {
                    hunter.SetBehaviour(null);
                    hunter.StopMovement();
                    boid.Collect();
                    claimedBoid = boid;
                    hunter.GatherStatus = $"Recolectando: 0.0/{hunter.GatherDuration:0.0} s";
                }
                else
                {
                    arrive.SetTargetNew(target, Mathf.Max(2f, hunter.GatherRange * 2f), hunter.GatherRange * 0.5f);
                    hunter.SetBehaviour(arrive);
                }
            }
            else if (Time.time >= nextRouteAttempt)
            {
                if (hunter.Navigation == null || !hunter.Navigation.TryFollowPosition(target.position))
                {
                    sensor.IgnoreTemporarily(target);
                    ReturnToSearch();
                    return;
                }
                nextRouteAttempt = Time.time + 0.75f;
            }
            return;
        }

        hunter.StopMovement();
        gatherTimer += Time.deltaTime;
        hunter.GatherStatus = $"Recolectando: {gatherTimer:0.0}/{hunter.GatherDuration:0.0} s";
        if (gatherTimer >= hunter.GatherDuration)
        {
            completed = true;
            claimedBoid.Disappear();
            hunter.RegisterGather();
            hunter.ClearTarget();
            hunterFSM.ChangeState(hunter.HasCompletedHunting ? HunterStates.Exit : HunterStates.Search);
        }
    }

    private void StopNavigation()
    {
        if (hunter.Navigation != null && hunter.Navigation.IsFollowingPath)
            hunter.Navigation.StopFollowingPath();
    }

    private void ReturnToSearch()
    {
        hunter.ClearTarget();
        hunterFSM.ChangeState(HunterStates.Search);
    }

    public override void Exit()
    {
        StopNavigation();
        if (!completed && claimedBoid != null)
            claimedBoid.CancelCollection();
        claimedBoid = null;
        hunter.SetBehaviour(null);
        hunter.StopMovement();
        hunter.GatherStatus = "-";
    }
}
