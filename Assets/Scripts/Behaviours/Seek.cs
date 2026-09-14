using UnityEngine;

public class Seek : SteeringBehaviour
{
    [SerializeField] private Transform target;

    public override Vector3 CalculateSteering()
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Vector3 direction = target.position - transform.position;
        direction.Normalize();

        return direction;
    }
}
