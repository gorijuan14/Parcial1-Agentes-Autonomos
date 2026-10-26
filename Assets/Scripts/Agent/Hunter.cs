using UnityEngine;

public enum HunterStates
{
    Search,
    Attack,
    Gather,
    Exit
}

public class Hunter : Agent
{
    [Header("Attack")]
    [SerializeField] private float tba = 2f;
    [SerializeField] private float rangeAttackRadius = 5f;
    [SerializeField] private float meleeAttackRadius = 0.5f;

    [Header("References")]
    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public bool CanAttack => attackTimer >= tba;
    public AStarMovement Navigation => aStarMovement;
    public bool IsInHuntingZone => Bounds.Instance != null && Bounds.Instance.Contains(transform.position, 0.55f);
    [SerializeField, Min(0f)] private float gatherDuration = 2f;
    public float GatherDuration => gatherDuration;
    public int GatherCount { get; private set; }
    [Header("Objective")]
    [SerializeField, Min(1)] private int requiredGathers = 3;
    [SerializeField] private ZoneGizmo initialZone;
    [SerializeField] private ZoneGizmo exitZone;
    [SerializeField, Min(0.1f)] private float exitRestartDelay = 0.5f;
    [SerializeField] private bool showStatus = true;
    public int RequiredGathers => requiredGathers;
    public bool HasCompletedHunting => GatherCount >= requiredGathers;
    public float ExitRestartDelay => exitRestartDelay;
    public ZoneGizmo ExitZone => exitZone;
    public int CompletedCycles { get; private set; }
    public int SuccessfulAttacks { get; private set; }
    private int lastAttackDamage;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private Renderer[] renderers;
    private Collider[] colliders;
    private bool[] rendererStates;
    private bool[] colliderStates;
    private bool isVisible = true;
    private float attackTimer;
    private HunterFSM hunterFSM;
    private Transform currentTarget;
    public Transform CurrentTarget => currentTarget;


    [Header("Bait")]
    [SerializeField] private GameObject baitPrefab;
    private int activeBaits;
    [SerializeField, Range(0, 5)] private int maxBaits = 5;
    [SerializeField, Min(0.1f)] private float baitSpawnInterval = 10f;
    [SerializeField, Min(0f)] private float baitSearchDelay = 3f;
    [SerializeField, Min(0f)] private float baitPlacementDuration = 0.25f;
    [SerializeField, Min(0f)] private float baitMinSpacing = 3f;
    public float BaitSearchDelay => baitSearchDelay;
    public float BaitPlacementDuration => baitPlacementDuration;
    private float baitTimer;
    private bool isPlacingBait;

    private void Awake()
    {
        hunterFSM = new HunterFSM();

        HunterSearchState searchState = new HunterSearchState(hunterFSM, this);
        HunterAttackState attackState = new HunterAttackState(hunterFSM, this);
        HunterGatherState gatherState = new HunterGatherState(hunterFSM, this);

        hunterFSM.RegisterState(HunterStates.Search, searchState);
        hunterFSM.RegisterState(HunterStates.Attack, attackState);
        hunterFSM.RegisterState(HunterStates.Gather, gatherState);
        hunterFSM.RegisterState(HunterStates.Exit, new HunterExitState(hunterFSM, this));

        hunterFSM.ChangeState(HunterStates.Search);
    }

    protected override void Start()
    {
        base.Start();
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        rendererStates = new bool[renderers.Length];
        colliderStates = new bool[colliders.Length];
    }

    protected override void Update()
    {
        attackTimer += Time.deltaTime;
        hunterFSM.Update();
        
        if (!isPlacingBait && isVisible)
        {
            base.Update();
        }
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }
    

    public bool UpdateBaits()
    {
        if (HasCompletedHunting || !IsInHuntingZone || baitPrefab == null || activeBaits >= maxBaits)
        {
            return false;
        }

        baitTimer += Time.deltaTime;

        if (baitTimer < baitSpawnInterval)
        {
            return false;
        }

        return HasSpaceForBait();
    }

    public void SpawnBait()
    {
        if (HasCompletedHunting || !IsInHuntingZone || baitPrefab == null ||
            activeBaits >= maxBaits || !HasSpaceForBait())
        {
            return;
        }

        if (baitPrefab.GetComponent<Bait>() == null)
        {
            Debug.LogWarning($"{name}: el prefab de cebo necesita un componente Bait.", this);
            return;
        }

        baitTimer = 0f;

        Vector3 spawnPosition = Bounds.Instance.ClampPosition(transform.position, 1f);

        GameObject baitObject = Instantiate(
            baitPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Bait bait = baitObject.GetComponent<Bait>();

        if (bait != null)
        {
            bait.SetOwner(this);
            activeBaits++;
        }
    }
    
    public void RemoveBait()
    {
        activeBaits--;

        if (activeBaits < 0)
        {
            activeBaits = 0;
        }
    }

    public void SetBaitPlacementPaused(bool paused)
    {
        isPlacingBait = paused;
        if (paused)
        {
            StopMovement();
        }
    }

    public void RegisterAttack(int damage)
    {
        attackTimer = 0f;
        SuccessfulAttacks++;
        lastAttackDamage = damage;
    }

    public void FaceTarget(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), maxTurnSpeed * Time.deltaTime);
    }

    public void RegisterGather()
    {
        GatherCount++;
    }

    private bool HasSpaceForBait()
    {
        Vector3 position = Bounds.Instance.ClampPosition(transform.position, 1f);
        foreach (Collider nearby in Physics.OverlapSphere(position, baitMinSpacing,
            ~0, QueryTriggerInteraction.Ignore))
        {
            Bait bait = nearby.GetComponentInParent<Bait>();
            if (bait != null && !bait.IsDestroyed)
                return false;
        }
        return true;
    }

    public void SetSimulationVisible(bool visible)
    {
        if (visible == isVisible)
            return;
        isVisible = visible;
        StopMovement();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!visible)
                rendererStates[i] = renderers[i].enabled;
            renderers[i].enabled = visible && rendererStates[i];
        }
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!visible)
                colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = visible && colliderStates[i];
        }
    }

    public void RestartCycle()
    {
        Vector3 position = initialZone != null ? initialZone.Center : startingPosition;
        position.y = startingPosition.y;
        transform.SetPositionAndRotation(position, startingRotation);
        GatherCount = 0;
        SuccessfulAttacks = 0;
        lastAttackDamage = 0;
        attackTimer = 0f;
        baitTimer = 0f;
        CompletedCycles++;
        ClearTarget();
        StopMovement();
        SetSimulationVisible(true);
    }

    private void OnGUI()
    {
        if (showStatus && hunterFSM != null)
            GUI.Box(new Rect(12f, 12f, 280f, 115f),
                $"Cazador: {hunterFSM.CurrentState}\nRecolecciones: {GatherCount}/{requiredGathers}\n" +
                $"Ataques: {SuccessfulAttacks} | Ultimo dano: {lastAttackDamage}\n" +
                $"TBA restante: {Mathf.Max(0f, tba - attackTimer):0.0} s\nCiclos completados: {CompletedCycles}");
    }
}
