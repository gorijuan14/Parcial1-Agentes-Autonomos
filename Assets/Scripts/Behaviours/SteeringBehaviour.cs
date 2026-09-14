using UnityEngine;


public enum SteeringBehaviourType
{
    None,
    Seek,
    Arrive,
    Evade,
    Pursue,
    Flocking
}

public abstract class SteeringBehaviour : MonoBehaviour
{
    public abstract Vector3 CalculateSteering();
}
