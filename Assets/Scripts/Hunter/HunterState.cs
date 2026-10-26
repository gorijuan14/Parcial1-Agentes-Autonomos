using UnityEngine;

public abstract class HunterState
{
    protected HunterFSM hunterFSM;
    protected Hunter hunter;

    protected HunterState(HunterFSM hunterFSM, Hunter hunter)
    {
        this.hunterFSM = hunterFSM;
        this.hunter = hunter;
    }

    public abstract void Enter();

    public abstract void Update();

    public abstract void Exit();

    protected bool TryEnterGather(HunterSensor sensor)
    {
        if (hunter.HasCompletedHunting)
            return false;

        // A just-killed target is already known, even before the sensor's next scan.
        Boid currentBoid = hunter.CurrentTarget != null ? hunter.CurrentTarget.GetComponent<Boid>() : null;
        Transform target = currentBoid != null && currentBoid.IsDead && !currentBoid.IsCollected &&
            currentBoid.isActiveAndEnabled && sensor.CanSee(currentBoid.transform)
            ? currentBoid.transform : sensor.GetClosestDeadBoid();
        if (target == null)
            return false;

        if (hunter.CurrentTarget != null)
            hunter.CurrentTarget.GetComponent<Boid>()?.HideHealthBar();

        hunter.SetBehaviour(null);
        hunter.StopMovement();
        hunter.SetTarget(target);
        hunterFSM.ChangeState(HunterStates.Gather);
        return true;
    }
}
