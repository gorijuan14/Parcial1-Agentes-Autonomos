using UnityEngine;

public class Boid : Agent
{
    [Header("References")]
    private Patrol patrol;
    private Flocking flocking;
    private Evade evade;
    private Arrive arrive;
    private BoidSensor sensor;
    private Bait currentBait;
    private bool isCollected;

    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;
    private bool isDead;
    public bool IsDead => isDead;

    [Header("Bait")]
    [SerializeField] private float baitDamageInterval = 1f;
    [SerializeField] private float baitDamageDistance = 0.5f;
    private float baitDamageTimer;

    protected override void Start()
    {
        base.Start();

        currentHealth = maxHealth;

        evade = GetComponent<Evade>();
        arrive = GetComponent<Arrive>();
        patrol = GetComponent<Patrol>();
        flocking = GetComponent<Flocking>();
        sensor = GetComponentInChildren<BoidSensor>();
    }

    protected override void Update()
    {
        if (isCollected || isDead)
        {
            return;
        }

        Transform hunter = sensor.GetClosestHunter();

        if (hunter != null && evade != null)
        {
            evade.SetTarget(hunter);
            
            Vector3 _steering = evade.CalculateSteering();
            Move(_steering);

            return;
        }

        if (currentBait == null)
        {
            Bait bait = sensor.GetClosestBait();

            if (bait != null && bait.IsAvailable)
            {
                if (bait.TryAssignBoid(this))
                {
                    currentBait = bait;
                    arrive.SetTarget(bait.transform);
                    baitDamageTimer = 0f;
                }
            }
        }

        Vector3 steering = Vector3.zero;

        if (currentBait != null)
        {
            float distance = Vector3.Distance(
                transform.position,
                currentBait.transform.position
            );

            if (distance > baitDamageDistance)
            {
                steering = arrive.CalculateSteering();
            }
            else
            {
                StopMovement();

                baitDamageTimer += Time.deltaTime;

                if (baitDamageTimer >= baitDamageInterval)
                {
                    baitDamageTimer = 0f;
                    currentBait.TakeDamage(1);
                }

                if (currentBait == null || currentBait.IsDestroyed)
                {
                    currentBait = null;
                    baitDamageTimer = 0f;
                }
            }
        }
        else
        {
            if (patrol != null)
            {
                steering += patrol.CalculateSteering();
            }

            if (flocking != null)
            {
                steering += flocking.CalculateSteering();
            }
        }

        Move(steering);
    }

    public void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;
        StopMovement();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        StopMovement();
    }

    public void Disappear()
    {
        gameObject.SetActive(false);
    }
}
