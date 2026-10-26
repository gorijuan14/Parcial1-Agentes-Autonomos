using UnityEngine;

public class HunterSensor : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float perceptionRadius = 7f;

    [Header("Detection")]
    [SerializeField] private float detectionInterval = 0.2f;
    [SerializeField] private LayerMask obstacleMask = 1 << 6;
    [SerializeField] private float sightHeight = 0.6f;

    private float detectionTimer;
    private Transform closestBoid;
    private Transform ignoredTarget;
    private float ignoreUntil;

    public bool CanSee(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        Vector3 offset = target.position - transform.position;
        if (offset.sqrMagnitude > perceptionRadius * perceptionRadius)
            return false;

        return !Physics.Linecast(transform.position + Vector3.up * sightHeight,
            target.position + Vector3.up * sightHeight, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    public void IgnoreTemporarily(Transform target)
    {
        ignoredTarget = target;
        ignoreUntil = Time.time + 2f;
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
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            perceptionRadius
        );

        closestBoid = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponentInParent<Boid>();

            if (boid == null || boid.IsDead || !boid.isActiveAndEnabled ||
                (boid.transform == ignoredTarget && Time.time < ignoreUntil) || !CanSee(boid.transform))
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                boid.transform.position
            );

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
            if (boid == null || boid.IsDead || !boid.isActiveAndEnabled || !CanSee(closestBoid))
                closestBoid = null;
        }

        return closestBoid;
    }
}
