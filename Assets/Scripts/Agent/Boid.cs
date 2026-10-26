using UnityEngine;
using System.Collections;

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
    private StateIndicator stateIndicator;
    private BoidHealthBar healthBar;


    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;
    private bool isDead;
    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [Header("Respawn")]
    [SerializeField] private float respawnMinX = -20f;
    [SerializeField] private float respawnMaxX = 20f;
    [SerializeField] private float respawnMinZ = -20f;
    [SerializeField] private float respawnMaxZ = 20f;

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
        stateIndicator = GetComponentInChildren<StateIndicator>();
        healthBar = GetComponentInChildren<BoidHealthBar>(true);

        if (healthBar != null)
        {
            healthBar.Hide();
        }
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
            FinishEatingBait();
            evade.SetTarget(hunter);

            Vector3 _steering = evade.CalculateSteering();

            if (flocking != null)
            {
                _steering += flocking.CalculateSteering();
            }

            stateIndicator.SetBoidEvade();

            Move(_steering);
            ApplyBounds();

            return;
        }

        if (currentBait == null || currentBait.IsDestroyed || !currentBait.isActiveAndEnabled)
        {
            FinishEatingBait();
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
            stateIndicator.SetBoidDistracted();
            float distance = Vector3.Distance(
                transform.position,
                currentBait.transform.position
            );
            
            if (distance > baitDamageDistance)
            {
                baitDamageTimer = 0f;
                // Stop inside eating range without changing patrol's Arrive settings.
                steering = arrive.CalculateSteering(
                    Mathf.Max(1f, baitDamageDistance * 2f),
                    baitDamageDistance * 0.5f);
            }
            else
            {
                StopMovement();

                stateIndicator.SetBoidDistracted();

                baitDamageTimer += Time.deltaTime;

                if (baitDamageTimer >= baitDamageInterval)
                {
                    baitDamageTimer = 0f;
                    currentBait.TakeDamage(1);
                    if (currentBait.IsDestroyed)
                    {
                        FinishEatingBait();
                    }
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

            stateIndicator.SetBoidPatrol();
        }

        Move(steering);

        ApplyBounds();
    }

    private void FinishEatingBait()
    {
        if (currentBait != null)
        {
            currentBait.ReleaseBoid(this);
        }

        currentBait = null;
        baitDamageTimer = 0f;
    }

    private void OnDisable()
    {
        FinishEatingBait();
    }

    public void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;
        FinishEatingBait();
        StopMovement();
    }

    public void ShowHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.Show();
        }
    }

    public void HideHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.Hide();
        }
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
        FinishEatingBait();

        StopMovement();
    }

    public void Disappear()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isCollected = true;
        FinishEatingBait();
        StopMovement();

        yield return null;

        Vector3 respawnPosition = new Vector3(
            Random.Range(respawnMinX, respawnMaxX),
            transform.position.y,
            Random.Range(respawnMinZ, respawnMaxZ)
        );

        transform.position = respawnPosition;

        currentHealth = maxHealth;
        isDead = false;
        isCollected = false;
    }

}
