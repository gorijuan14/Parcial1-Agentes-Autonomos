using UnityEngine;

public class HunterSensor : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float perceptionRadius = 15f;

    public Transform GetClosestBoid()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            perceptionRadius
        );

        Transform closestBoid = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponent<Boid>();

            if (boid == null)
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

        return closestBoid;
    }
}
