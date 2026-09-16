using UnityEngine;

public class BoidSensor : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float baitPerceptionRadius = 7f;
    [SerializeField] private float hunterPerceptionRadius = 5f;

    public Bait GetClosestBait()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            baitPerceptionRadius
        );

        Bait closestBait = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            Bait bait = collider.GetComponent<Bait>();

            if (bait == null)
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

        return closestBait;
    }

    public Transform GetClosestHunter()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            hunterPerceptionRadius
        );

        Transform closestHunter = null;
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

        return closestHunter;
    }
}
