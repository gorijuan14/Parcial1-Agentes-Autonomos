using UnityEngine;


public enum SteeringBehaviourType
{
    None,
    Seek,
    Arrive,
    Evade,
    Separation
}

public abstract class SteeringBehaviour : MonoBehaviour
{
    public abstract Vector3 CalculateSteering();
}
