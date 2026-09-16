using UnityEngine;

public class HunterAttackState : HunterState
{
    [Header("References")]
    private Agent agent;
    private Pursue pursue;
    private HunterSensor sensor;
    private Hunter hunterAgent;
    [SerializeField] private Transform target;

    [Header("Stats")]
    private float attackCooldown;
    
    public HunterAttackState(HunterFSM hunter, Transform target) : base(hunter)
    {
        agent = hunter.GetComponent<Agent>();
        hunterAgent = hunter.GetComponent<Hunter>();
        pursue = hunter.GetComponent<Pursue>();
        sensor = hunter.GetComponentInChildren<HunterSensor>();

        this.target = target;
    }

    public override void Enter()
    {
        attackCooldown = 0f;

        pursue.SetTarget(target);
        agent.SetBehaviour(pursue);
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
                targetDied = TryRangedAttack();
            }
        }
        else
        {
            agent.SetBehaviour(null);
            agent.StopMovement();

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
            target = boid;
            pursue.SetTarget(target);
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
    }

}
