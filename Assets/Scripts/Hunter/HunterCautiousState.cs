using UnityEngine;

public class HunterCautiousState : HunterState
{
    private readonly StateIndicator indicator;
    private float waitingTime;
    private float clearTime;
    private float nextRouteAttempt;
    private bool takingDetour;

    public HunterCautiousState(HunterFSM fsm, Hunter hunter) : base(fsm, hunter)
    {
        indicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        if (hunter.CurrentTarget != null)
            hunter.CurrentTarget.GetComponent<Boid>()?.HideHealthBar();
        hunter.ClearTarget();
        hunter.SetBaitPlacementPaused(false);
        hunter.AvoidWolvesDuringNavigation = true;
        hunter.EmergencyEvasion = false;
        hunter.SetBehaviour(null);
        hunter.StopMovement();
        if (hunter.Navigation != null)
            hunter.Navigation.StopFollowingPath();
        waitingTime = 0f;
        clearTime = 0f;
        nextRouteAttempt = 0f;
        takingDetour = false;
        indicator.SetHunterCautious();
    }

    public override void Update()
    {
        if (takingDetour && hunter.Navigation.IsFollowingPath)
        {
            if (Time.time >= nextRouteAttempt)
            {
                takingDetour = hunter.Navigation.TryFollowPosition(hunter.CautionDestination);
                if (!takingDetour)
                {
                    hunter.Navigation.StopFollowingPath();
                    hunter.SetBehaviour(null);
                    hunter.StopMovement();
                }
                nextRouteAttempt = Time.time + 1f;
            }
            return;
        }

        Wolf blocking = hunter.FindBlockingUnawareWolf(hunter.CautionDirection, 1f);
        if (blocking == null)
        {
            clearTime += Time.deltaTime;
            if (clearTime >= hunter.CautionReleaseDelay)
                hunterFSM.ChangeState(hunter.HasCompletedHunting ? HunterStates.Exit : HunterStates.Search);
            return;
        }

        clearTime = 0f;
        waitingTime += Time.deltaTime;
        hunter.FaceTarget(blocking.transform);
        if (!takingDetour)
        {
            hunter.SetBehaviour(null);
            hunter.StopMovement();
        }

        // First let the patrol pass; if it remains in the way, try an A* detour
        // with the same wolf exclusion rules used while fleeing.
        if (waitingTime >= hunter.CautionRerouteDelay && Time.time >= nextRouteAttempt && hunter.Navigation != null)
        {
            takingDetour = hunter.Navigation.TryFollowPosition(hunter.CautionDestination);
            nextRouteAttempt = Time.time + 1f;
        }
    }

    public override void Exit()
    {
        if (hunter.Navigation != null && hunter.Navigation.IsFollowingPath)
            hunter.Navigation.StopFollowingPath();
        hunter.AvoidWolvesDuringNavigation = false;
        hunter.EmergencyEvasion = false;
        hunter.SetBehaviour(null);
        hunter.StopMovement();
    }
}
