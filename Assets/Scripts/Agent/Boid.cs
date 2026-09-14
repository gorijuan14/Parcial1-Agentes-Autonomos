using UnityEngine;

public class Boid : Agent
{
    private Patrol patrol;
    private Flocking flocking;
    private bool isCollected;

    protected override void Start()
    {
        base.Start();

        patrol = GetComponent<Patrol>();
        flocking = GetComponent<Flocking>();
    }

    protected override void Update()
    {
        if (isCollected)
        {
            return;
        }

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

    public void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;
        StopMovement();
    }


    public void Disappear()
    {
        gameObject.SetActive(false);
    }
}