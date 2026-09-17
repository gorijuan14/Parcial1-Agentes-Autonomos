using UnityEngine;

public class HunterPatrolState : HunterState
{
    [Header("References")]
    private Patrol patrol;
    private HunterSensor sensor;
    private Hunter hunterAgent;
    private StateIndicator stateIndicator;

    [Header("Bait")]
    private float baitPlacementTimer;
    private bool isPlacingBait;

    public HunterPatrolState(HunterFSM hunter) : base(hunter)
    {
        patrol = hunter.GetComponent<Patrol>();
        sensor = hunter.GetComponentInChildren<HunterSensor>();
        hunterAgent = hunter.GetComponent<Hunter>();
        stateIndicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        hunter.GetComponent<Hunter>().SetBehaviour(patrol);

        stateIndicator.SetHunterPatrol();
    }

    public override void Update()
    {
        if (isPlacingBait)
        {
            baitPlacementTimer += Time.deltaTime;

            hunter.GetComponent<Agent>().StopMovement();

            if (baitPlacementTimer >= 1f)
            {
                hunterAgent.SpawnBait();
                stateIndicator.SetHunterPatrol();
                isPlacingBait = false;
            }

            return;
        }

        if (hunterAgent.ShouldSpawnBait())
        {
            isPlacingBait = true;
            baitPlacementTimer = 0f;
            stateIndicator.SetHunterBait();
            hunter.GetComponent<Agent>().StopMovement();

            return;
        }

        Transform boid = sensor.GetClosestBoid();

        if (boid != null)
        {
            hunter.ChangeState(
                new HunterAttackState(hunter, boid)
            );
        }


    }
    public override void Exit()
    {
    }
}