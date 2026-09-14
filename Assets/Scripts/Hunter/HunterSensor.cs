using UnityEngine;

public class HunterSensor : MonoBehaviour
{
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
            SteeringAgent agent = collider.GetComponent<SteeringAgent>();

            if (agent == null)
            {
                continue;
            }

            if (agent.gameObject == gameObject)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                agent.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBoid = agent.transform;
            }
        }

        return closestBoid;
    }
}
