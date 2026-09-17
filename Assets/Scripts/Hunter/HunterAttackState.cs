using UnityEngine;

public class HunterAttackState : HunterState
{
    [Header("References")]
    private Agent agent;
    private Pursue pursue;
    private HunterSensor sensor;
    private Hunter hunterAgent;
    private StateIndicator stateIndicator;
    [SerializeField] private Transform target;

    [Header("Stats")]
    private float attackCooldown;
    
    public HunterAttackState(HunterFSM hunter, Transform target) : base(hunter)
    {
        agent = hunter.GetComponent<Agent>();
        hunterAgent = hunter.GetComponent<Hunter>();
        pursue = hunter.GetComponent<Pursue>();
        sensor = hunter.GetComponentInChildren<HunterSensor>();
        stateIndicator = hunter.GetComponentInChildren<StateIndicator>();

        this.target = target;
    }

    public override void Enter()
    {
        attackCooldown = 0f;

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

        if (target == null)
        {
            hunter.ChangeState(
                new HunterPatrolState(hunter)
            );

            return;
        }

        Boid targetBoid = target.GetComponent<Boid>();

        if (targetBoid != null && targetBoid.IsDead)
        {
            hunter.ChangeState(
                new HunterGatherState(hunter, target)
            );

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
                targetDied = TryRangedAttack();
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
            targetDied = TryMeleeAttack();
        }

        if (targetDied)
        {
            hunter.ChangeState(
                new HunterGatherState(hunter, target)
            );

            return;
        }

        Transform boid = sensor.GetClosestBoid();

        if (boid == null)
        {
            hunter.ChangeState(
                new HunterPatrolState(hunter)
            );

            return;
        }

        if (boid != target)
        {
            Boid oldBoid = target != null
                ? target.GetComponent<Boid>()
                : null;

            if (oldBoid != null)
            {
                oldBoid.HideHealthBar();
            }

            target = boid;

            pursue.SetTarget(target);

            Boid newBoid = target.GetComponent<Boid>();

            if (newBoid != null)
            {
                newBoid.ShowHealthBar();
            }
        }
    }

    private bool TryMeleeAttack()
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

    private bool TryRangedAttack()
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
