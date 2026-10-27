using UnityEngine;

public class BoidSensor : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float baitPerceptionRadius = 7f;
    [SerializeField] private float hunterPerceptionRadius = 5f;

    [Header("Detection")]
    [SerializeField] private float detectionInterval = 0.2f;

    private float detectionTimer;

    private Bait closestBait;
    private Transform closestHunter;

    private void OnDrawGizmosSelected()
    {
        Color previousColor = Gizmos.color;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hunterPerceptionRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, baitPerceptionRadius);
        Gizmos.color = previousColor;
    }

    private void Update()
    {
        detectionTimer += Time.deltaTime;

        if (detectionTimer < detectionInterval)
        {
            return;
        }

        detectionTimer = 0f;

        DetectClosestHunter();
    }

    private void DetectClosestBait()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            baitPerceptionRadius
        );

        closestBait = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            Bait bait = collider.GetComponentInParent<Bait>();

            if (bait == null || !bait.IsAvailable)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                bait.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBait = bait;
            }
        }
    }

    private void DetectClosestHunter()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            hunterPerceptionRadius
        );

        closestHunter = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            Hunter hunter = collider.GetComponentInParent<Hunter>();

            if (hunter == null)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                hunter.transform.position
            );

            if (distance > hunterPerceptionRadius)
            {
                continue;
            }

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHunter = hunter.transform;
            }
        }
    }

    public Bait GetClosestBait()
    {
        // Another boid may have reserved the cached bait since the last scan.
        DetectClosestBait();
        return closestBait;
    }

    public Transform GetClosestHunter()
    {
        return closestHunter;
    }
}
