using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum WolfState { Patrol, Chase, Search }

[RequireComponent(typeof(Arrive), typeof(AStarMovement))]
public class Wolf : Agent
{
    // Alerts carry a snapshot, never a continuously updated hidden position.
    public static event Action<Wolf, Hunter, Vector3, int> Howled;
    [Header("References")]
    [SerializeField] private Hunter hunter;
    [SerializeField] private GraphNode[] patrolNodes;
    [SerializeField] private ZoneGizmo[] safeZones;
    [Header("Initial placement")]
    [SerializeField] private bool randomizeStartPosition = true;
    [SerializeField, Min(1f)] private float initialSpawnSeparation = 2f;
    [Header("Perception")]
    [SerializeField, Min(0.1f)] private float visionRadius = 9f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 110f;
    [SerializeField, Min(0f)] private float captureRadius = 0.85f;
    [Header("Movement")]
    [SerializeField, Min(0f)] private float patrolSpeed = 3.2f;
    [SerializeField, Min(0f)] private float chaseSpeed = 4.25f;
    [SerializeField, Min(0f)] private float searchSpeed = 3.6f;
    [Header("Search and alert")]
    [SerializeField, Min(0.1f)] private float searchDuration = 8f;
    [SerializeField, Min(0.1f)] private float searchRadius = 7f;
    [SerializeField, Min(0.1f)] private float howlInterval = 2f;
    private Arrive arrive;
    private Renderer body;
    private MaterialPropertyBlock bodyColor;
    private int patrolIndex;
    private float nextRouteTime;
    private float searchStartedTime;
    private bool reachedLastKnownPosition;
    private float lastHowlTime = -100f;
    private float lastAlertTime = -100f;
    private Vector3 lastKnownPosition;
    private Vector3 lastBroadcastPosition;
    private bool hasKnownPosition;
    private int observedLife;
    private GraphNode searchNode;
    public WolfState State { get; private set; }
    public bool SeesHunter { get; private set; }
    public float CaptureRadius => captureRadius;
    public int AlertsSent { get; private set; }
    public int AlertsReceived { get; private set; }

    public bool DetectsHunter(Hunter target)
    {
        // Evaluate current perception rather than the previous Update's cached flag.
        return isActiveAndEnabled && hunter != null && hunter == target && CanSeeHunter();
    }

    private void OnEnable() => Howled += ReceiveAlert;
    private void OnDisable() => Howled -= ReceiveAlert;

    protected override void Start()
    {
        base.Start();
        arrive = GetComponent<Arrive>();
        body = GetComponentInChildren<Renderer>();
        bodyColor = new MaterialPropertyBlock();
        if (hunter == null)
            hunter = FindFirstObjectByType<Hunter>();
        if (randomizeStartPosition)
            RandomizeInitialPosition();
        State = WolfState.Patrol;
        UpdateColor();
    }

    private void RandomizeInitialPosition()
    {
        if (patrolNodes == null || patrolNodes.Length == 0)
            return;

        var candidates = new List<GraphNode>();
        foreach (GraphNode node in patrolNodes)
            if (CanUseNode(node) && !candidates.Contains(node))
                candidates.Add(node);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int other = UnityEngine.Random.Range(0, i + 1);
            GraphNode temporary = candidates[i];
            candidates[i] = candidates[other];
            candidates[other] = temporary;
        }

        Wolf[] wolves = FindObjectsByType<Wolf>(FindObjectsSortMode.None);
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        Vector3 scale = transform.lossyScale;
        float radius = capsule != null
            ? capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) : 0.5f;
        float halfSegment = capsule != null
            ? Mathf.Max(0f, capsule.height * Mathf.Abs(scale.y) * 0.5f - radius) : 0.5f;
        Vector3 centerOffset = capsule != null ? transform.TransformVector(capsule.center) : Vector3.up;

        foreach (GraphNode node in candidates)
        {
            Vector3 position = node.Position;
            position.y = transform.position.y;
            Vector3 center = position + centerOffset;
            if (Physics.CheckCapsule(center + Vector3.up * halfSegment, center - Vector3.up * halfSegment,
                radius, obstacleMask, QueryTriggerInteraction.Ignore))
                continue;

            bool occupied = false;
            foreach (Wolf wolf in wolves)
            {
                if (wolf == this || !wolf.isActiveAndEnabled)
                    continue;
                Vector3 offset = wolf.transform.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < initialSpawnSeparation * initialSpawnSeparation)
                {
                    occupied = true;
                    break;
                }
            }
            if (occupied)
                continue;

            transform.position = position;
            patrolIndex = (Array.IndexOf(patrolNodes, node) + 1) % patrolNodes.Length;
            GraphNode next = patrolNodes[patrolIndex];
            if (next != null)
            {
                Vector3 direction = next.Position - position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(direction);
            }
            StopMovement();
            return;
        }
        Debug.LogWarning($"{name}: no hay un punto inicial libre en su patrulla; se conserva la posicion de escena.", this);
    }

    protected override void Update()
    {
        if (hunter == null)
            return;
        if (hasKnownPosition && (!hunter.IsVisible || hunter.IsInSafeZone || hunter.LifeVersion != observedLife))
            ForgetHunter();

        SeesHunter = CanSeeHunter();
        if (SeesHunter)
        {
            bool newlyDetected = !hasKnownPosition || observedLife != hunter.LifeVersion;
            lastKnownPosition = hunter.transform.position;
            observedLife = hunter.LifeVersion;
            hasKnownPosition = true;
            reachedLastKnownPosition = false;
            searchNode = null;
            ChangeState(WolfState.Chase);
            if (Time.time - lastHowlTime >= howlInterval &&
                (newlyDetected || Time.time - lastHowlTime >= howlInterval * 3f ||
                 (lastKnownPosition - lastBroadcastPosition).sqrMagnitude >= 4f || AlertsSent == 0))
            {
                lastHowlTime = Time.time;
                lastBroadcastPosition = lastKnownPosition;
                AlertsSent++;
                Howled?.Invoke(this, hunter, lastKnownPosition, observedLife);
            }
            if (Vector3.Distance(transform.position, hunter.transform.position) <= captureRadius)
            {
                hunter.MarkCaptured();
                ForgetHunter();
            }
        }
        else if (State == WolfState.Chase)
        {
            ChangeState(WolfState.Search);
        }

        float configuredSpeed = State == WolfState.Chase ? chaseSpeed : State == WolfState.Search ? searchSpeed : patrolSpeed;
        maxSpeed = Mathf.Min(configuredSpeed, hunter.MaxSpeed * 0.85f);
        if (State == WolfState.Patrol)
            Patrol();
        else if (State == WolfState.Chase)
            Navigate(lastKnownPosition, false);
        else
            Search();

        Vector3 previous = transform.position;
        base.Update();
        // Avoidance may deviate from a planned edge: enforce the safe-zone boundary too.
        if (CrossesSafeZone(previous, transform.position))
        {
            transform.position = previous;
            StopMovement();
        }
    }

    private bool CanSeeHunter()
    {
        if (!hunter.IsVisible || hunter.IsCaptured || hunter.IsInSafeZone)
            return false;
        Vector3 offset = hunter.transform.position - transform.position;
        if (offset.sqrMagnitude > visionRadius * visionRadius)
            return false;
        offset.y = 0f;
        if (offset.sqrMagnitude > 0.0001f && Vector3.Angle(transform.forward, offset) > viewAngle * 0.5f)
            return false;
        return !Physics.Linecast(transform.position + Vector3.up * eyeHeight,
            hunter.transform.position + Vector3.up * eyeHeight, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void ReceiveAlert(Wolf sender, Hunter target, Vector3 position, int life)
    {
        if (sender == this || !isActiveAndEnabled || target != hunter ||
            !target.IsVisible || target.IsCaptured || target.IsInSafeZone || CanSeeHunter())
            return;
        lastKnownPosition = position;
        observedLife = life;
        hasKnownPosition = true;
        reachedLastKnownPosition = false;
        lastAlertTime = Time.time;
        searchNode = null;
        AlertsReceived++;
        ChangeState(WolfState.Search);
        nextRouteTime = 0f;
    }

    private void Patrol()
    {
        if (patrolNodes == null || patrolNodes.Length == 0)
        {
            SetBehaviour(null);
            StopMovement();
            return;
        }
        GraphNode target = patrolNodes[patrolIndex];
        if (target == null || !CanUseNode(target) || Vector3.Distance(transform.position, target.Position) <= 0.65f)
        {
            patrolIndex = (patrolIndex + 1) % patrolNodes.Length;
            StopNavigation();
            nextRouteTime = 0f;
            target = patrolNodes[patrolIndex];
        }
        if (target != null && CanUseNode(target))
            Navigate(target.Position, false);
    }

    private void Search()
    {
        if (!hasKnownPosition)
        {
            ForgetHunter();
            return;
        }
        if (!reachedLastKnownPosition)
        {
            if (Vector3.Distance(transform.position, lastKnownPosition) > 0.75f)
            {
                if (!Navigate(lastKnownPosition, true))
                    ForgetHunter();
                return;
            }
            reachedLastKnownPosition = true;
            searchStartedTime = Time.time;
        }
        if (Time.time - searchStartedTime >= searchDuration)
        {
            ForgetHunter();
            return;
        }
        if (searchNode == null || Vector3.Distance(transform.position, searchNode.Position) <= 0.75f)
        {
            var candidates = new List<GraphNode>();
            if (aStarMovement.Graph != null)
                foreach (GraphNode node in aStarMovement.Graph.Nodes)
                    if (CanUseNode(node) && (node.Position - lastKnownPosition).sqrMagnitude <= searchRadius * searchRadius &&
                        (node.Position - transform.position).sqrMagnitude > 1f)
                        candidates.Add(node);
            if (candidates.Count == 0)
            {
                StopNavigation();
                StopMovement();
                SetBehaviour(null);
                return;
            }
            searchNode = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            nextRouteTime = 0f;
        }
        Navigate(searchNode.Position, true);
    }

    private bool Navigate(Vector3 destination, bool forcePath)
    {
        if (!forcePath && CanMoveDirectlyTo(destination))
        {
            StopNavigation();
            arrive.SetPositionTarget(destination, 1.2f, 0.25f);
            SetBehaviour(arrive);
        }
        else if (Time.time >= nextRouteTime)
        {
            if (!aStarMovement.TryFollowPosition(destination))
            {
                StopNavigation();
                SetBehaviour(null);
                StopMovement();
                nextRouteTime = Time.time + 0.75f;
                return false;
            }
            nextRouteTime = Time.time + 0.75f;
        }
        return true;
    }

    private void ChangeState(WolfState state)
    {
        if (State == state)
            return;
        StopNavigation();
        SetBehaviour(null);
        StopMovement();
        State = state;
        nextRouteTime = 0f;
        UpdateColor();
    }

    private void ForgetHunter()
    {
        hasKnownPosition = false;
        SeesHunter = false;
        searchNode = null;
        ChangeState(WolfState.Patrol);
    }

    private void StopNavigation()
    {
        if (aStarMovement != null && aStarMovement.IsFollowingPath)
            aStarMovement.StopFollowingPath();
    }

    public override bool CanUseNode(GraphNode node)
    {
        return base.CanUseNode(node) && !CrossesSafeZone(node.Position, node.Position);
    }

    public override bool CanTraverseSegment(Vector3 from, Vector3 to)
    {
        return !CrossesSafeZone(from, to) && base.CanTraverseSegment(from, to);
    }

    private bool CrossesSafeZone(Vector3 from, Vector3 to)
    {
        if (safeZones != null)
            foreach (ZoneGizmo zone in safeZones)
                if (zone != null && zone.IntersectsSegment(from, to, 0.55f))
                    return true;
        return false;
    }

    private void UpdateColor()
    {
        if (body == null)
            return;
        Color color = State == WolfState.Chase ? Color.red : State == WolfState.Search ? Color.yellow : Color.gray;
        body.GetPropertyBlock(bodyColor);
        bodyColor.SetColor("_BaseColor", color);
        bodyColor.SetColor("_Color", color);
        body.SetPropertyBlock(bodyColor);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = SeesHunter ? Color.red : Color.yellow;
        Vector3 previous = origin + Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * visionRadius;
        Gizmos.DrawLine(origin, previous);
        for (int i = 1; i <= 24; i++)
        {
            Vector3 next = origin + Quaternion.Euler(0f, -viewAngle * 0.5f + viewAngle * i / 24f, 0f) * transform.forward * visionRadius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
        Gizmos.DrawLine(origin, previous);
        Gizmos.DrawWireSphere(transform.position, captureRadius);
        if (hasKnownPosition)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, lastKnownPosition + Vector3.up * 0.2f);
            Gizmos.DrawWireSphere(lastKnownPosition, 0.4f);
        }
    }
#if UNITY_EDITOR
    public void DrawSceneOverlay(SceneView view)
    {
        if (Vector3.Dot(view.camera.transform.forward, transform.position - view.camera.transform.position) <= 0f)
            return;
        Vector2 point = HandleUtility.WorldToGUIPoint(transform.position + Vector3.up * 2.5f);
        string action = Time.time - lastHowlTime < 1f ? "AULLIDO" : Time.time - lastAlertTime < 1f ? "ALERTA RECIBIDA" : State.ToString();
        GUI.color = State == WolfState.Chase ? Color.red : Color.white;
        GUI.Label(new Rect(point.x - 65f, point.y - 15f, 160f, 45f), $"{name}: {action}\nAlertas {AlertsSent}/{AlertsReceived}");
    }
#endif
}
