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

    public HunterPatrolState(HunterFSM hunterFSM, Hunter hunter) : base(hunterFSM, hunter)
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

            hunter.StopMovement();

            if (baitPlacementTimer >= 1f)
            {
                hunterAgent.SpawnBait();
                stateIndicator.SetHunterPatrol();
                isPlacingBait = false;
            }

            return;
        }

        if (hunterAgent.UpdateBaits())
        {
            isPlacingBait = true;
            baitPlacementTimer = 0f;

            stateIndicator.SetHunterBait();
            hunter.StopMovement();

            return;
        }

        Transform boid = sensor.GetClosestBoid();

       if (boid != null)
        {
            hunter.SetTarget(boid);
            hunterFSM.ChangeState(HunterStates.Attack);
        }

    }
    public override void Exit()
    {
    }
}