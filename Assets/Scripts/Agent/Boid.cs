using UnityEngine;

public class Boid : Agent
{
    private Patrol patrol;
    private Flocking flocking;
    private bool isCollected;

    [SerializeField] private int maxHealth = 3;

    private int currentHealth;
    private bool isDead;
    public bool IsDead => isDead;

    protected override void Start()
    {
        base.Start();

        currentHealth = maxHealth;

        patrol = GetComponent<Patrol>();
        flocking = GetComponent<Flocking>();
    }

    protected override void Update()
    {
        if (isCollected)
        {
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

        Debug.Log($"{name} recibió {damage} de daño. HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        StopMovement();

        Debug.Log($"{name} murió.");
    }

    public void Disappear()
    {
        gameObject.SetActive(false);
    }
}
