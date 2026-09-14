using UnityEngine;

public class Boid : Agent
{
    private Patrol patrol;
    private Flocking flocking;

    protected override void Start()
    {
        base.Start();

        patrol = GetComponent<Patrol>();
        flocking = GetComponent<Flocking>();
    }

    protected override void Update()
    {
        Vector3 steering = Vector3.zero;

        if (patrol != null)
        {
            steering += patrol.CalculateSteering();
        }

        if (flocking != null)
        {
            steering += flocking.CalculateSteering();
        }

        Move(steering);
    }
}