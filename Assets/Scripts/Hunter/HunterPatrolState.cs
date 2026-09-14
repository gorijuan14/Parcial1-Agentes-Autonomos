using UnityEngine;

public class HunterPatrolState : HunterState
{
    private Patrol patrol;
    private HunterSensor sensor;

    public HunterPatrolState(HunterFSM hunter) : base(hunter)
    {
        patrol = hunter.GetComponent<Patrol>();
        sensor = hunter.GetComponent<HunterSensor>();
    }

    public override void Enter()
    {
        hunter.GetComponent<SteeringAgent>().SetBehaviour(patrol);
    }

    public override void Update()
    {
        Transform boid = sensor.GetClosestBoid();

        if (boid != null)
        {
            Debug.Log($"Patrol detectó a {boid.name}");
        }
    }

    public override void Exit()
    {
    }
}