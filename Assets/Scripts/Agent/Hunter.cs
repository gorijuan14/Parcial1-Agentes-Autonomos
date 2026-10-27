using UnityEngine;

public enum HunterStates
{
    Search,
    Attack,
    Gather,
    Exit,
    Flee,
    Dead,
    Cautious
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
    [SerializeField, Min(0.1f)] private float gatherRange = 1.25f;
    [SerializeField, Min(1f)] private float gatherStuckTimeout = 5f;
    [SerializeField, Min(1f)] private float gatherApproachTimeout = 45f;
    public float GatherRange => gatherRange;
    public float GatherStuckTimeout => gatherStuckTimeout;
    public float GatherApproachTimeout => gatherApproachTimeout;
    public string GatherStatus { get; set; }
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
    [Header("Wolves and refuges")]
    [SerializeField] private ZoneGizmo[] safeZones;
    [SerializeField, Min(0f)] private float respawnTime = 5f;
    [SerializeField, Min(0f)] private float safeZoneWaitTime = 4f;
    [SerializeField, Range(0f, 0.3f)] private float fleeRouteTolerance = 0.1f;
    public float FleeRouteTolerance => fleeRouteTolerance;
    [SerializeField, Min(1f)] private float wolfSafetyRadius = 2.25f;
    [SerializeField, Min(0.1f)] private float wolfPredictionTime = 0.35f;
    public bool AvoidWolvesDuringNavigation { get; set; }
    public bool EmergencyEvasion { get; set; }
    [Header("Caution around unaware wolves")]
    [SerializeField, Min(1f)] private float cautionDistance = 5f;
    [SerializeField, Min(0.5f)] private float cautionCorridorWidth = 1.5f;
    [SerializeField, Min(0f)] private float cautionReleaseDelay = 1f;
    [SerializeField, Min(0f)] private float cautionRerouteDelay = 2f;
    public float CautionReleaseDelay => cautionReleaseDelay;
    public float CautionRerouteDelay => cautionRerouteDelay;
    public Vector3 CautionDirection { get; set; }
    public Vector3 CautionDestination { get; set; }
    public Vector3 IntendedDestination => CurrentTarget != null ? CurrentTarget.position :
        Navigation != null && Navigation.IsFollowingPath ? Navigation.Destination : transform.position;
    public Vector3 IntendedDirection => CurrentTarget != null ? CurrentTarget.position - transform.position :
        Navigation != null && Navigation.CurrentTargetNode != null
            ? Navigation.CurrentTargetNode.Position - transform.position : velocity;

    public Wolf FindBlockingUnawareWolf(Vector3 direction, float extraDistance = 0f)
    {
        Wolf closest = null;
        float bestDistance = float.PositiveInfinity;
        foreach (Wolf wolf in VisibleWolves)
        {
            if (wolf == null || wolf.DetectsHunter(this) ||
                !ThreatAvoidance.BlocksTravel(transform.position, direction, wolf.transform.position,
                    cautionDistance + extraDistance, cautionCorridorWidth))
                continue;
            float distance = (wolf.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                closest = wolf;
                bestDistance = distance;
            }
        }
        return closest;
    }
    private int threatFrame = -1;
    private float threatSafetyRadius;
    private readonly System.Collections.Generic.List<Vector3> threatPositions =
        new System.Collections.Generic.List<Vector3>();
    public System.Collections.Generic.IReadOnlyList<Wolf> VisibleWolves => sensor != null
        ? sensor.GetVisibleWolves() : System.Array.Empty<Wolf>();
    public ZoneGizmo[] SafeZones => safeZones;
    public float RespawnTime => respawnTime;
    public float SafeZoneWaitTime => safeZoneWaitTime;
    public bool IsVisible => isVisible;
    public bool IsCaptured { get; private set; }
    public int LifeVersion { get; private set; }
    public int Deaths { get; private set; }
    public ZoneGizmo SelectedSafeZone { get; set; }
    private HunterSensor sensor;
    public bool HasVisibleWolf => sensor != null && sensor.GetClosestWolf() != null;
    public bool IsSpottedByVisibleWolf
    {
        get
        {
            foreach (Wolf wolf in VisibleWolves)
                if (wolf != null && wolf.DetectsHunter(this))
                    return true;
            return false;
        }
    }
    public bool IsInSafeZone
    {
        get
        {
            if (safeZones != null)
                foreach (ZoneGizmo zone in safeZones)
                    if (zone != null && zone.Contains(transform.position))
                        return true;
            return false;
        }
    }
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
        InitializeRuntime();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
            InitializeRuntime();
    }

    private void OnDisable()
    {
        // Release claimed bodies and navigation before Unity reloads managed state.
        hunterFSM?.Stop();
        hunterFSM = null;
        SetBehaviour(null);
        StopMovement();
    }

    private void CachePresentationReferences()
    {
        if (renderers == null || rendererStates == null)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            rendererStates = new bool[renderers.Length];
        }
        if (colliders == null || colliderStates == null)
        {
            colliders = GetComponentsInChildren<Collider>(true);
            colliderStates = new bool[colliders.Length];
        }
    }

    private void InitializeRuntime()
    {
        if (hunterFSM != null)
            return;
        InitializeAgentReferences();
        CachePresentationReferences();
        sensor = GetComponentInChildren<HunterSensor>();
        hunterFSM = new HunterFSM(this);

        HunterSearchState searchState = new HunterSearchState(hunterFSM, this);
        HunterAttackState attackState = new HunterAttackState(hunterFSM, this);
        HunterGatherState gatherState = new HunterGatherState(hunterFSM, this);

        hunterFSM.RegisterState(HunterStates.Search, searchState);
        hunterFSM.RegisterState(HunterStates.Attack, attackState);
        hunterFSM.RegisterState(HunterStates.Gather, gatherState);
        hunterFSM.RegisterState(HunterStates.Exit, new HunterExitState(hunterFSM, this));
        hunterFSM.RegisterState(HunterStates.Flee, new HunterFleeState(hunterFSM, this));
        hunterFSM.RegisterState(HunterStates.Dead, new HunterDeadState(hunterFSM, this));
        hunterFSM.RegisterState(HunterStates.Cautious, new HunterCautiousState(hunterFSM, this));

        hunterFSM.ChangeState(IsCaptured ? HunterStates.Dead :
            HasCompletedHunting ? HunterStates.Exit : HunterStates.Search);
    }

    protected override void Start()
    {
        base.Start();
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        CachePresentationReferences();
    }

    protected override void Update()
    {
        // Domain reload can discard the non-serialized FSM without invoking Awake again.
        if (hunterFSM == null)
            InitializeRuntime();
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

    public void MarkCaptured()
    {
        if (isVisible && !IsInSafeZone)
        {
            IsCaptured = true;
            StopMovement();
        }
    }

    private System.Collections.Generic.IReadOnlyList<Vector3> GetThreatPositions()
    {
        if (threatFrame == Time.frameCount)
            return threatPositions;
        threatFrame = Time.frameCount;
        threatPositions.Clear();
        threatSafetyRadius = wolfSafetyRadius;
        foreach (Wolf wolf in VisibleWolves)
        {
            if (wolf == null || !wolf.isActiveAndEnabled)
                continue;
            threatSafetyRadius = Mathf.Max(threatSafetyRadius, wolf.CaptureRadius + 0.75f);
            threatPositions.Add(wolf.transform.position);
            threatPositions.Add(wolf.transform.position + wolf.Velocity * wolfPredictionTime);
        }
        return threatPositions;
    }

    public override bool CanUseNode(GraphNode node)
    {
        if (!base.CanUseNode(node))
            return false;
        if (AvoidWolvesDuringNavigation)
            foreach (Vector3 threat in GetThreatPositions())
                if (!ThreatAvoidance.IsSegmentSafe(node.Position, node.Position, threat, threatSafetyRadius))
                    return false;
        return true;
    }

    public override bool CanTraverseSegment(Vector3 from, Vector3 to)
    {
        if (!base.CanTraverseSegment(from, to))
            return false;
        if (AvoidWolvesDuringNavigation)
            foreach (Vector3 threat in GetThreatPositions())
                if (!ThreatAvoidance.IsSegmentSafe(from, to, threat, threatSafetyRadius))
                    return false;
        return true;
    }

    protected override Vector3 ConstrainVelocity(Vector3 proposed)
    {
        if (!AvoidWolvesDuringNavigation || IsInSafeZone)
            return proposed;
        var threats = GetThreatPositions();
        return ThreatAvoidance.SafeVelocity(transform.position, proposed, threats,
            threatSafetyRadius, Mathf.Max(0.6f, Time.deltaTime), maxSpeed, EmergencyEvasion,
            base.CanTraverseSegment);
    }

    public void RestartCycle(bool completed = true)
    {
        Vector3 position = initialZone != null ? initialZone.Center : startingPosition;
        position.y = startingPosition.y;
        transform.SetPositionAndRotation(position, startingRotation);
        GatherCount = 0;
        SuccessfulAttacks = 0;
        lastAttackDamage = 0;
        attackTimer = 0f;
        baitTimer = 0f;
        if (completed)
            CompletedCycles++;
        else
            Deaths++;
        IsCaptured = false;
        LifeVersion++;
        SelectedSafeZone = null;
        ClearTarget();
        StopMovement();
        SetSimulationVisible(true);
    }

    private void OnGUI()
    {
        if (showStatus && hunterFSM != null)
            GUI.Box(new Rect(12f, 12f, 330f, 215f),
                $"Cazador: {hunterFSM.CurrentState}\nRecolecciones: {GatherCount}/{requiredGathers}\n" +
                $"Ataques: {SuccessfulAttacks} | Ultimo dano: {lastAttackDamage}\n" +
                $"TBA restante: {Mathf.Max(0f, tba - attackTimer):0.0} s\nCiclos completados: {CompletedCycles}\n" +
                $"Capturas: {Deaths} | Lobo visible: {(HasVisibleWolf ? "SI" : "NO")}\n" +
                $"Detectado por un lobo: {(IsSpottedByVisibleWolf ? "SI" : "NO")}\n" +
                $"Refugio: {(SelectedSafeZone != null ? SelectedSafeZone.name : "-")}\n" +
                $"Evasion de emergencia: {(EmergencyEvasion ? "SI" : "NO")}\n" +
                $"Recoleccion: {GatherStatus}");
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
        if (AvoidWolvesDuringNavigation)
        {
            Gizmos.color = new Color(1f, 0.45f, 0f);
            foreach (Vector3 threat in GetThreatPositions())
                Gizmos.DrawWireSphere(threat, threatSafetyRadius);
        }
        if (SelectedSafeZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, SelectedSafeZone.Center);
        }
    }
}
