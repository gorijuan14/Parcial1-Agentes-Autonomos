using UnityEngine;

public class HunterDeadState : HunterState
{
    private float timer;
    public HunterDeadState(HunterFSM fsm, Hunter hunter) : base(fsm, hunter) { }
    public override void Enter()
    {
        timer = 0f;
        if (hunter.Navigation != null)
            hunter.Navigation.StopFollowingPath();
        hunter.ClearTarget();
        hunter.SetBaitPlacementPaused(false);
        hunter.SetBehaviour(null);
        hunter.SetSimulationVisible(false);
    }
    public override void Update()
    {
        timer += Time.deltaTime;
        if (timer >= hunter.RespawnTime)
        {
            hunter.RestartCycle(false);
            hunterFSM.ChangeState(HunterStates.Search);
        }
    }
    public override void Exit()
    {
        hunter.SetSimulationVisible(true);
    }
}