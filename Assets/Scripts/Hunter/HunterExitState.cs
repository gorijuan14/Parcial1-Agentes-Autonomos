using UnityEngine;

public class HunterExitState : HunterState
{
    private readonly StateIndicator indicator;
    private bool restarting;
    private float restartTimer;
    private float nextRouteAttempt;

    public HunterExitState(HunterFSM fsm, Hunter hunter) : base(fsm, hunter)
    {
        indicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        restarting = false;
        restartTimer = 0f;
        nextRouteAttempt = 0f;
        hunter.ClearTarget();
        hunter.SetBaitPlacementPaused(false);
        hunter.SetBehaviour(null);
        hunter.StopMovement();
        indicator.SetHunterExit();
    }

    public override void Update()
    {
        if (restarting)
        {
            restartTimer += Time.deltaTime;
            if (restartTimer >= hunter.ExitRestartDelay)
            {
                hunter.RestartCycle();
                hunterFSM.ChangeState(HunterStates.Search);
            }
            return;
        }

        if (hunter.ExitZone == null || hunter.Navigation == null)
            return;

        Vector3 destination = hunter.ExitZone.Center;
        destination.y = hunter.transform.position.y;
        if (Vector3.Distance(hunter.transform.position, destination) <= 1f)
        {
            hunter.Navigation.StopFollowingPath();
            hunter.SetSimulationVisible(false);
            restarting = true;
            return;
        }

        if (!hunter.Navigation.IsFollowingPath && Time.time >= nextRouteAttempt)
        {
            if (!hunter.Navigation.TryFollowPosition(destination))
                Debug.LogWarning($"{hunter.name}: no hay un camino A* transitable hasta la salida.", hunter);
            nextRouteAttempt = Time.time + 1f;
        }
    }

    public override void Exit()
    {
        if (hunter.Navigation != null && hunter.Navigation.IsFollowingPath)
            hunter.Navigation.StopFollowingPath();
        hunter.SetBehaviour(null);
        hunter.StopMovement();
        hunter.SetSimulationVisible(true);
    }
}
