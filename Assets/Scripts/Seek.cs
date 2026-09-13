using UnityEngine;

public class Seek : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private SteeringAgent agent;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<SteeringAgent>();
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction = direction.normalized;

        agent.SetSteering(direction);
    }
}
