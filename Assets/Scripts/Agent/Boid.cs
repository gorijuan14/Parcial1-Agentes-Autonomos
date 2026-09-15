using UnityEngine;

public class Boid : Agent
{
    private Patrol patrol;
    private Flocking flocking;
    private bool isCollected;
    private BoidSensor sensor;
    private Arrive arrive;

    [SerializeField] private int maxHealth = 3;

    private int currentHealth;
    private bool isDead;
    public bool IsDead => isDead;

    protected override void Start()
    {
        base.Start();

        currentHealth = maxHealth;

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

        Transform bait = sensor.GetClosestBait();

        if (bait != null && arrive != null)
        {
            arrive.SetTarget(bait);

            SetBehaviour(arrive);

            return;
        }

        Vector3 steering = Vector3.zero;

        if (patrol != null)
        {
            steering += patrol.CalculateSteering();
        }

        if (flocking != null)
        {
            steering += flocking.CalculateSteering();
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
