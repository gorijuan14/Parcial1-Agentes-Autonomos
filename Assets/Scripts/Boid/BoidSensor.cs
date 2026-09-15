using UnityEngine;

public class BoidSensor : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float perceptionRadius = 7f;

    public Bait GetClosestBait()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            perceptionRadius
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
}
