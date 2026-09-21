using UnityEngine;

public class HunterAttackState : HunterState
{
    [Header("References")]
    private Agent agent;
    private Pursue pursue;
    private HunterSensor sensor;
    private Hunter hunterAgent;
    private StateIndicator stateIndicator;

    [Header("Stats")]
    private float attackCooldown;

    public HunterAttackState(HunterFSM hunterFSM, Hunter hunter) : base(hunterFSM, hunter)
    {
        agent = hunter.GetComponent<Agent>();
        hunterAgent = hunter.GetComponent<Hunter>();
        pursue = hunter.GetComponent<Pursue>();
        sensor = hunter.GetComponentInChildren<HunterSensor>();
        stateIndicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        attackCooldown = 0f;

        Transform target = hunterAgent.CurrentTarget;

        if (target == null)
        {
            hunterFSM.ChangeState(HunterStates.Patrol);
            return;
        }

        pursue.SetTarget(target);
        agent.SetBehaviour(pursue);

        Boid boid = target.GetComponent<Boid>();

        if (boid != null)
        {
            boid.ShowHealthBar();
        }

        stateIndicator.SetHunterChase();
    }

    public override void Update()
    {
        attackCooldown += Time.deltaTime;

        Transform target = hunterAgent.CurrentTarget;

        // No tenemos objetivo
        if (target == null)
        {
            hunterFSM.ChangeState(HunterStates.Patrol);
            return;
        }

        Boid targetBoid = target.GetComponent<Boid>();

        // El objetivo murió
        if (targetBoid != null && targetBoid.IsDead)
        {
            hunterFSM.ChangeState(HunterStates.Gather);
            return;
        }

        float distance = Vector3.Distance(
            hunter.transform.position,
            target.position
        );

        bool targetDied = false;

        if (distance > hunterAgent.MeleeAttackRadius)
        {
            agent.SetBehaviour(pursue);

            if (distance <= hunterAgent.RangeAttackRadius)
            {
                stateIndicator.SetHunterRangedAttack();
                targetDied = TryRangedAttack(target);
            }
            else
            {
                stateIndicator.SetHunterChase();
            }
        }
        else
        {
            agent.SetBehaviour(null);
            agent.StopMovement();

            stateIndicator.SetHunterMeleeAttack();
            targetDied = TryMeleeAttack(target);
        }

        if (targetDied)
        {
            hunterFSM.ChangeState(HunterStates.Gather);
            return;
        }

        // Buscar si apareció un objetivo más cercano
        Transform boid = sensor.GetClosestBoid();

        if (boid == null)
        {
            hunterAgent.ClearTarget();
            hunterFSM.ChangeState(HunterStates.Patrol);
            return;
        }

        if (boid != target)
        {
            Boid oldBoid = target.GetComponent<Boid>();

            if (oldBoid != null)
            {
                oldBoid.HideHealthBar();
            }

            hunterAgent.SetTarget(boid);

            pursue.SetTarget(boid);

            Boid newBoid = boid.GetComponent<Boid>();

            if (newBoid != null)
            {
                newBoid.ShowHealthBar();
            }
        }
    }

    private bool TryMeleeAttack(Transform target)
    {
        if (attackCooldown < hunterAgent.TBA)
        {
            return false;
        }

        Boid boid = target.GetComponent<Boid>();

        if (boid != null)
        {
            boid.TakeDamage(3);
        }

        attackCooldown = 0f;

        return boid != null && boid.IsDead;
    }

    private bool TryRangedAttack(Transform target)
    {
        if (attackCooldown < hunterAgent.TBA)
        {
            return false;
        }

        Boid boid = target.GetComponent<Boid>();

        if (boid != null)
        {
            boid.TakeDamage(1);
        }

        attackCooldown = 0f;

        return boid != null && boid.IsDead;
    }

    public override void Exit()
    {
        Transform target = hunterAgent.CurrentTarget;

        if (target != null)
        {
            Boid boid = target.GetComponent<Boid>();

            if (boid != null)
            {
                boid.HideHealthBar();
            }
        }
    }
}
