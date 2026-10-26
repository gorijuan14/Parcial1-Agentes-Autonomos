using UnityEngine;

public class HunterAttackState : HunterState
{
    private readonly Pursue pursue;
    private readonly HunterSensor sensor;
    private readonly StateIndicator stateIndicator;
    private readonly AStarMovement navigation;
    private float attackCooldown;
    private float lastSeenTime;
    private Vector3 lastSeenPosition;
    private float nextRepathTime;
    private Vector3 progressPosition;
    private float lastProgressTime;
    private const float TargetMemoryDuration = 5f;
    private const float RepathInterval = 0.75f;
    private const float StuckTimeout = 3f;

    public HunterAttackState(HunterFSM hunterFSM, Hunter hunter) : base(hunterFSM, hunter)
    {
        pursue = hunter.GetComponent<Pursue>();
        sensor = hunter.GetComponentInChildren<HunterSensor>();
        stateIndicator = hunter.GetComponentInChildren<StateIndicator>();
        navigation = hunter.GetComponent<AStarMovement>();
    }

    public override void Enter()
    {
        attackCooldown = 0f;
        nextRepathTime = 0f;
        lastProgressTime = Time.time;
        progressPosition = hunter.transform.position;
        Transform target = hunter.CurrentTarget;
        if (target == null || sensor == null || !sensor.CanSee(target))
        {
            AbandonTarget();
            return;
        }

        RememberTarget(target);
        pursue.SetTarget(target);
        hunter.SetBehaviour(pursue);
        target.GetComponent<Boid>()?.ShowHealthBar();
        stateIndicator.SetHunterChase();
    }

    public override void Update()
    {
        attackCooldown += Time.deltaTime;
        Transform target = hunter.CurrentTarget;
        Boid boid = target != null ? target.GetComponent<Boid>() : null;
        if (boid == null || !boid.isActiveAndEnabled)
        {
            AbandonTarget();
            return;
        }

        if (boid.IsDead)
        {
            hunterFSM.ChangeState(HunterStates.Gather);
            return;
        }

        // Retarget only to a currently visible, living boid.
        Transform closest = sensor.GetClosestBoid();
        if (closest != null && closest != target)
        {
            boid.HideHealthBar();
            StopNavigation();
            hunter.SetTarget(closest);
            target = closest;
            boid = target.GetComponent<Boid>();
            boid.ShowHealthBar();
            pursue.SetTarget(target);
            nextRepathTime = 0f;
            lastProgressTime = Time.time;
            progressPosition = hunter.transform.position;
        }

        bool visible = sensor.CanSee(target);
        if (visible)
            RememberTarget(target);
        else if (Time.time - lastSeenTime >= TargetMemoryDuration)
        {
            AbandonTarget();
            return;
        }

        Vector3 destination = lastSeenPosition;
        float distance = Vector3.Distance(hunter.transform.position, destination);
        if (visible && distance <= hunter.MeleeAttackRadius)
        {
            StopNavigation();
            hunter.SetBehaviour(null);
            hunter.StopMovement();
            lastProgressTime = Time.time;
            stateIndicator.SetHunterMeleeAttack();
            TryAttack(boid, 3);
            return;
        }

        stateIndicator.SetHunterChase();
        if (hunter.CanMoveDirectlyTo(destination))
        {
            StopNavigation();
            if (!visible)
            {
                // Search the last observed location, never the hidden target's live position.
                if (distance <= 0.5f)
                {
                    AbandonTarget();
                    return;
                }
                Arrive arrive = hunter.GetComponent<Arrive>();
                arrive.SetPositionTarget(destination, 2f, 0.3f);
                hunter.SetBehaviour(arrive);
            }
            else
            {
                pursue.SetTarget(target);
                hunter.SetBehaviour(pursue);
            }
        }
        else if (Time.time >= nextRepathTime || navigation == null || !navigation.IsFollowingPath)
        {
            if (navigation == null || !navigation.TryFollowPosition(destination))
            {
                AbandonTarget();
                return;
            }
            nextRepathTime = Time.time + RepathInterval;
        }

        if (visible && distance <= hunter.RangeAttackRadius)
        {
            stateIndicator.SetHunterRangedAttack();
            TryAttack(boid, 1);
            if (boid.IsDead)
                return;
        }

        if ((hunter.transform.position - progressPosition).sqrMagnitude >= 0.0625f)
        {
            progressPosition = hunter.transform.position;
            lastProgressTime = Time.time;
        }
        else if (Time.time - lastProgressTime >= StuckTimeout)
        {
            AbandonTarget();
        }
    }

    private void RememberTarget(Transform target)
    {
        lastSeenPosition = target.position;
        lastSeenTime = Time.time;
    }

    private void TryAttack(Boid boid, int damage)
    {
        // Recheck sight at the point where damage is applied.
        if (attackCooldown < hunter.TBA || !sensor.CanSee(boid.transform))
            return;

        boid.TakeDamage(damage);
        attackCooldown = 0f;
        if (boid.IsDead)
            hunterFSM.ChangeState(HunterStates.Gather);
    }

    private void StopNavigation()
    {
        if (navigation != null && navigation.IsFollowingPath)
            navigation.StopFollowingPath();
    }

    private void AbandonTarget()
    {
        Transform target = hunter.CurrentTarget;
        if (target != null)
        {
            target.GetComponent<Boid>()?.HideHealthBar();
            sensor?.IgnoreTemporarily(target);
        }
        hunter.ClearTarget();
        hunterFSM.ChangeState(HunterStates.Patrol);
    }

    public override void Exit()
    {
        StopNavigation();
        pursue.SetTarget(null);
        if (hunter.CurrentTarget != null)
            hunter.CurrentTarget.GetComponent<Boid>()?.HideHealthBar();
    }
}