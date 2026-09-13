using UnityEngine;

public class Separation : SteeringBehaviour
{
    [SerializeField] private float perceptionRadius = 5f;
    [SerializeField] private float separationRadius = 2f;

    public override Vector3 CalculateSteering()
    {
        Collider[] neighbors = Physics.OverlapSphere(
            transform.position,
            perceptionRadius
        );

        Vector3 separation = Vector3.zero;

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform)
            {
                continue;
            }

            Vector3 direction = transform.position - neighbor.transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance > 0f && distance < separationRadius)
            {
                separation += direction.normalized / distance;
            }
        }

        return separation;
    }
}
