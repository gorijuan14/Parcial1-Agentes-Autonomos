using UnityEngine;

public class HunterAttackState : HunterState
{
    private readonly HunterSensor sensor;
    private readonly Pursue pursue;
    private readonly StateIndicator indicator;
    private float nextRouteAttempt;

    public HunterAttackState(HunterFSM fsm, Hunter hunter) : base(fsm, hunter)
    {
        sensor = hunter.GetComponentInChildren<HunterSensor>();
        pursue = hunter.GetComponent<Pursue>();
        indicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        if (TryEnterGather(sensor))
            return;

        nextRouteAttempt = 0f;
        Transform target = hunter.CurrentTarget;
        if (!hunter.CanAttack || target == null || !sensor.CanSee(target))
        {
            ReturnToSearch();
            return;
        }
        target.GetComponent<Boid>()?.ShowHealthBar();
        indicator.SetHunterChase();
    }

    public override void Update()
    {
        // An available corpse interrupts pursuit before another attack is applied.
        if (TryEnterGather(sensor))
            return;

        Transform target = hunter.CurrentTarget;
        Boid boid = target != null ? target.GetComponent<Boid>() : null;
        if (boid == null || boid.IsDead || boid.IsCollected || !sensor.CanSee(target))
        {
            ReturnToSearch();
            return;
        }

        float distance = Vector3.Distance(hunter.transform.position, target.position);
        if (distance <= hunter.RangeAttackRadius || distance <= hunter.MeleeAttackRadius)
        {
            hunter.FaceTarget(target);
            int damage;
            if (distance <= hunter.MeleeAttackRadius)
            {
                indicator.SetHunterMeleeAttack();
                damage = 3;
            }
            else
            {
                indicator.SetHunterRangedAttack();
                damage = 1;
            }
            boid.TakeDamage(damage);
            hunter.RegisterAttack(damage);
            if (boid.IsDead && TryEnterGather(sensor))
                return;

            ReturnToSearch(!boid.IsDead);
            return;
        }

        indicator.SetHunterChase();
        if (hunter.CanMoveDirectlyTo(target.position))
        {
            StopNavigation();
            pursue.SetTarget(target);
            hunter.SetBehaviour(pursue);
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
    }

    private void ReturnToSearch(bool keepTarget = false)
    {
        if (!keepTarget)
        {
            if (hunter.CurrentTarget != null)
                hunter.CurrentTarget.GetComponent<Boid>()?.HideHealthBar();
            hunter.ClearTarget();
        }
        hunterFSM.ChangeState(HunterStates.Search);
    }

    private void StopNavigation()
    {
        if (hunter.Navigation != null && hunter.Navigation.IsFollowingPath)
            hunter.Navigation.StopFollowingPath();
    }

    public override void Exit()
    {
        StopNavigation();
        pursue.SetTarget(null);
        hunter.SetBehaviour(null);
        if (hunter.CurrentTarget == null)
            hunter.StopMovement();
    }
}
