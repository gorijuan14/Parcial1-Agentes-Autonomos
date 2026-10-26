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
        indicator.SetHunterGather();
    }

    public override void Update()
    {
        Transform target = hunter.CurrentTarget;
        Boid boid = target != null ? target.GetComponent<Boid>() : null;
        if (boid == null || !boid.IsDead || !boid.gameObject.activeInHierarchy ||
            (claimedBoid == null && boid.IsCollected))
        {
            ReturnToSearch();
            return;
        }

        if (claimedBoid == null)
        {
            if (sensor.CanSee(target) && hunter.CanMoveDirectlyTo(target.position))
            {
                StopNavigation();
                if (Vector3.Distance(hunter.transform.position, target.position) <= 1f)
                {
                    hunter.SetBehaviour(null);
                    hunter.StopMovement();
                    boid.Collect();
                    claimedBoid = boid;
                }
                else
                {
                    arrive.SetTargetNew(target, 2f, 0.5f);
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
        if (gatherTimer >= hunter.GatherDuration)
        {
            completed = true;
            claimedBoid.Disappear();
            hunter.RegisterGather();
            ReturnToSearch();
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
    }
}