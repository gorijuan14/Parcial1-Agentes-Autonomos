
using UnityEngine;

public class Seek : SteeringBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public override Vector3 CalculateSteering()
    {
        if (target == null)
            return Vector3.zero;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        return direction.normalized;
    }
}
