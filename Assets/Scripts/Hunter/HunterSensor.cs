using UnityEngine;
using System.Collections.Generic;

public class HunterSensor : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float perceptionRadius = 7f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 90f;

    [Header("Detection")]
    [SerializeField] private float detectionInterval = 0.2f;
    [SerializeField] private LayerMask obstacleMask = 1 << 6;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private float sightHeight = 0.6f;

    private float detectionTimer;
    private Transform closestBoid;
    private Transform closestDeadBoid;
    private Transform ignoredTarget;
    private float ignoreUntil;
    private int lastDetectionFrame = -1;
    [Header("Wolf perception")]
    [SerializeField, Min(0.1f)] private float wolfPerceptionRadius = 8f;
    private int lastWolfFrame = -1;
    private Wolf closestWolf;
    private readonly List<Wolf> visibleWolves = new List<Wolf>();

    public IReadOnlyList<Wolf> GetVisibleWolves()
    {
        GetClosestWolf();
        return visibleWolves;
    }

    public Wolf GetClosestWolf()
    {
        if (lastWolfFrame == Time.frameCount)
            return closestWolf;
        lastWolfFrame = Time.frameCount;
        closestWolf = null;
        visibleWolves.Clear();
        float bestDistance = wolfPerceptionRadius * wolfPerceptionRadius;
        foreach (Collider candidate in Physics.OverlapSphere(transform.position, wolfPerceptionRadius,
            ~0, QueryTriggerInteraction.Ignore))
        {
            Wolf wolf = candidate.GetComponentInParent<Wolf>();
            if (wolf == null || !wolf.isActiveAndEnabled)
                continue;
            float distance = (wolf.transform.position - transform.position).sqrMagnitude;
            if (distance > wolfPerceptionRadius * wolfPerceptionRadius || Physics.Linecast(transform.position + Vector3.up * sightHeight,
                wolf.transform.position + Vector3.up * sightHeight, obstacleMask, QueryTriggerInteraction.Ignore))
                continue;
            if (!visibleWolves.Contains(wolf))
                visibleWolves.Add(wolf);
            if (distance <= bestDistance)
            {
                closestWolf = wolf;
                bestDistance = distance;
            }
        }
        return closestWolf;
    }

    public bool CanSee(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        Vector3 offset = target.position - transform.position;
        if (offset.sqrMagnitude > perceptionRadius * perceptionRadius)
            return false;

        if ((targetMask.value & (1 << target.gameObject.layer)) == 0)
            return false;

        offset.y = 0f;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (offset.sqrMagnitude > 0.0001f && Vector3.Angle(forward, offset) > viewAngle * 0.5f)
            return false;

        return !Physics.Linecast(transform.position + Vector3.up * sightHeight,
            target.position + Vector3.up * sightHeight, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    public void IgnoreTemporarily(Transform target, float duration = 2f)
    {
        ignoredTarget = target;
        ignoreUntil = Time.time + duration;
        closestBoid = null;
    }

    private void Update()
    {
        detectionTimer += Time.deltaTime;

        if (detectionTimer < detectionInterval)
        {
            return;
        }

        detectionTimer = 0f;

        DetectClosestBoid();
    }

    private void DetectClosestBoid()
    {
        lastDetectionFrame = Time.frameCount;
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            perceptionRadius,
            targetMask,
            QueryTriggerInteraction.Ignore
        );

        closestBoid = null;
        closestDeadBoid = null;

        float closestDistance = Mathf.Infinity;
        float closestDeadDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponentInParent<Boid>();

            if (boid == null || boid.IsCollected || !boid.isActiveAndEnabled ||
                (boid.transform == ignoredTarget && Time.time < ignoreUntil) || !CanSee(boid.transform))
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                boid.transform.position
            );

            if (boid.IsDead)
            {
                if (distance < closestDeadDistance)
                {
                    closestDeadDistance = distance;
                    closestDeadBoid = boid.transform;
                }
                continue;
            }

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBoid = boid.transform;
            }
        }
    }

    public Transform GetClosestBoid()
    {
        if (closestBoid != null)
        {
            Boid boid = closestBoid.GetComponent<Boid>();
            if (boid == null || boid.IsDead || boid.IsCollected || !boid.isActiveAndEnabled || !CanSee(closestBoid))
                closestBoid = null;
        }

        return closestBoid;
    }

    public Transform GetClosestDeadBoid()
    {
        // Do not start another hunt using the previous interval's corpse cache.
        if (lastDetectionFrame != Time.frameCount)
            DetectClosestBoid();

        if (closestDeadBoid != null)
        {
            Boid boid = closestDeadBoid.GetComponent<Boid>();
            if (boid == null || !boid.IsDead || boid.IsCollected || !boid.isActiveAndEnabled || !CanSee(closestDeadBoid))
                closestDeadBoid = null;
        }
        return closestDeadBoid;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = closestWolf != null ? StateIndicator.HunterWarning : StateIndicator.HunterGreen;
        Gizmos.DrawWireSphere(transform.position, wolfPerceptionRadius);
        if (closestWolf != null)
            Gizmos.DrawLine(transform.position, closestWolf.transform.position);
        Vector3 origin = transform.position + Vector3.up * sightHeight;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 previous = origin + Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * forward * perceptionRadius;
        Gizmos.color = StateIndicator.HunterYellow;
        Gizmos.DrawLine(origin, previous);
        const int segments = 32;
        for (int i = 1; i <= segments; i++)
        {
            float angle = -viewAngle * 0.5f + viewAngle * i / segments;
            Vector3 next = origin + Quaternion.Euler(0f, angle, 0f) * forward * perceptionRadius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
        Gizmos.DrawLine(origin, previous);
    }
}
