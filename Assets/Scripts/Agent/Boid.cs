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
    public bool IsCollected => isCollected;
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
    [SerializeField, Min(0f)] private float respawnDelay = 3f;
    [SerializeField, Min(0f)] private float boundaryPadding = 0.55f;
    private bool isRespawning;
    private Vector3 initialPosition;
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
        ApplyBounds();
        initialPosition = transform.position;

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

            stateIndicator.SetBoidEvade();

            MoveInsideZone(_steering);
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

        MoveInsideZone(steering);

        ApplyBounds();
    }

    private void MoveInsideZone(Vector3 steering)
    {
        if (Bounds.Instance != null)
        {
            Vector3 predicted = transform.position + velocity * 0.6f + steering * 0.1f;
            Vector3 correction = Bounds.Instance.ClampPosition(predicted, boundaryPadding) - predicted;
            correction.y = 0f;
            if (correction.sqrMagnitude > 0.0001f)
                steering += correction.normalized * maxAcceleration * 2f;
        }
        Move(steering);
    }

    protected override void ApplyBounds()
    {
        if (Bounds.Instance == null)
            return;

        Vector3 position = transform.position;
        Vector3 clamped = Bounds.Instance.ClampPosition(position, boundaryPadding);
        if (Mathf.Abs(position.x - clamped.x) > 0.0001f)
            velocity.x = 0f;
        if (Mathf.Abs(position.z - clamped.z) > 0.0001f)
            velocity.z = 0f;
        transform.position = clamped;
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

    public void CancelCollection()
    {
        if (isDead && !isRespawning)
            isCollected = false;
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
        if (isRespawning)
            return;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        isCollected = true;
        FinishEatingBait();
        StopMovement();

        HideHealthBar();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool[] rendererStates = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            rendererStates[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        bool[] colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }

        yield return new WaitForSeconds(respawnDelay);

        Vector3 respawnPosition = new Vector3(
            Random.Range(respawnMinX, respawnMaxX),
            transform.position.y,
            Random.Range(respawnMinZ, respawnMaxZ)
        );

        if (Bounds.Instance != null)
        {
            respawnPosition = initialPosition;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                Vector3 candidate = Bounds.Instance.RandomPosition(transform.position.y, boundaryPadding);
                if (!Physics.CheckSphere(candidate, boundaryPadding, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    respawnPosition = candidate;
                    break;
                }
            }
        }

        transform.position = respawnPosition;
        ApplyBounds();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = rendererStates[i];
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = colliderStates[i];

        currentHealth = maxHealth;
        isDead = false;
        isCollected = false;
        isRespawning = false;
    }

}
