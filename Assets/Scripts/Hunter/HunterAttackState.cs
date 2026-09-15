using UnityEngine;

public class HunterAttackState : HunterState
{
    private Agent agent;
    private Pursue pursue;
    private HunterSensor sensor;
    private Hunter hunterAgent;

    private float attackCooldown;
    [SerializeField] private Transform target;

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

        Debug.Log($"Hunter entra en Attack. Objetivo: {target.name}");
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

        float distance = Vector3.Distance(
            hunter.transform.position,
            target.position
        );

        if (distance > hunterAgent.MeleeAttackRadius)
        {
            agent.SetBehaviour(pursue);

            if (distance <= hunterAgent.RangeAttackRadius)
            {
                TryRangedAttack();
            }
        }
        else
        {
            agent.SetBehaviour(null);
            agent.StopMovement();

            TryMeleeAttack();
        }

        EvaluateAttackRange(distance);

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

    private void EvaluateAttackRange(float distance)
    {
        if (distance <= hunterAgent.MeleeAttackRadius)
        {
            Debug.Log("Hunter: MELEE");
        }
        else if (distance <= hunterAgent.RangeAttackRadius)
        {
            Debug.Log("Hunter: RANGED");
        }
        else
        {
            Debug.Log("Hunter: CHASE");
        }
    }

    private void TryMeleeAttack()
    {
        if (attackCooldown < hunterAgent.TBA)
        {
            return;
        }

        Debug.Log("HUNTER MELEE ATTACK!");

        Boid boid = target.GetComponent<Boid>();

        if (boid != null)
        {
            boid.TakeDamage(3);
        }

        attackCooldown = 0f;
    }

    private void TryRangedAttack()
    {
        if (attackCooldown < hunterAgent.TBA)
        {
            return;
        }

        Debug.Log("HUNTER RANGED ATTACK!");

        Boid boid = target.GetComponent<Boid>();

        if (boid != null)
        {
            boid.TakeDamage(1);
        }

        attackCooldown = 0f;
    }

    public override void Exit()
    {
    }

}
