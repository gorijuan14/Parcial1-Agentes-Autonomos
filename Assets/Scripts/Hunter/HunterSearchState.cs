using System.Collections.Generic;
using UnityEngine;

public class HunterSearchState : HunterState
{
    private readonly HunterSensor sensor;
    private readonly StateIndicator indicator;
    private bool isPlacingBait;
    private float baitPlacementTimer;
    private float nextRouteAttempt;
    private bool travellingToHuntingZone;

    public HunterSearchState(HunterFSM fsm, Hunter hunter) : base(fsm, hunter)
    {
        sensor = hunter.GetComponentInChildren<HunterSensor>();
        indicator = hunter.GetComponentInChildren<StateIndicator>();
    }

    public override void Enter()
    {
        isPlacingBait = false;
        baitPlacementTimer = 0f;
        nextRouteAttempt = 0f;
        travellingToHuntingZone = false;
        hunter.SetBaitPlacementPaused(false);
        hunter.SetBehaviour(null);
        hunter.StopMovement();
        indicator.SetHunterSearch();
    }

    public override void Update()
    {
        AStarMovement navigation = hunter.Navigation;
        Bounds area = Bounds.Instance;
        if (navigation == null || area == null)
            return;

        if (!hunter.IsInHuntingZone)
        {
            travellingToHuntingZone = true;
            if (!navigation.IsFollowingPath && Time.time >= nextRouteAttempt)
            {
                TrySearchRoute(navigation, area, true);
                nextRouteAttempt = Time.time + 1f;
            }
            return;
        }

        if (travellingToHuntingZone)
        {
            navigation.StopFollowingPath();
            travellingToHuntingZone = false;
        }

        // Search transitions have priority over waiting to place another bait.
        Transform deadBoid = sensor.GetClosestDeadBoid();
        if (deadBoid != null)
        {
            hunter.SetTarget(deadBoid);
            hunterFSM.ChangeState(HunterStates.Gather);
            return;
        }
        Transform liveBoid = sensor.GetClosestBoid();
        if (hunter.CanAttack && liveBoid != null)
        {
            hunter.SetTarget(liveBoid);
            hunterFSM.ChangeState(HunterStates.Attack);
            return;
        }

        if (isPlacingBait)
        {
            baitPlacementTimer += Time.deltaTime;
            if (baitPlacementTimer >= 1f)
            {
                hunter.SpawnBait();
                hunter.SetBaitPlacementPaused(false);
                isPlacingBait = false;
                indicator.SetHunterSearch();
            }
            return;
        }
        if (hunter.UpdateBaits())
        {
            navigation.StopFollowingPath();
            hunter.SetBaitPlacementPaused(true);
            isPlacingBait = true;
            baitPlacementTimer = 0f;
            indicator.SetHunterBait();
            return;
        }

        if (!navigation.IsFollowingPath && Time.time >= nextRouteAttempt)
        {
            TrySearchRoute(navigation, area, false);
            nextRouteAttempt = Time.time + 1f;
        }
    }

    private void TrySearchRoute(AStarMovement navigation, Bounds area, bool entering)
    {
        if (navigation.Graph == null)
            return;

        var candidates = new List<GraphNode>();
        foreach (GraphNode node in navigation.Graph.Nodes)
        {
            if (node != null && area.Contains(node.Position, 0.55f) &&
                (entering || (node.Position - hunter.transform.position).sqrMagnitude > 2.25f))
                candidates.Add(node);
        }
        if (entering)
            candidates.Sort((a, b) => (a.Position - area.Center).sqrMagnitude.CompareTo((b.Position - area.Center).sqrMagnitude));
        else
        {
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int other = Random.Range(0, i + 1);
                GraphNode temporary = candidates[i];
                candidates[i] = candidates[other];
                candidates[other] = temporary;
            }
        }
        foreach (GraphNode candidate in candidates)
        {
            if (navigation.TryFollowPosition(candidate.Position))
                return;
        }
        Debug.LogWarning($"{hunter.name}: no hay un camino A* transitable hasta un nodo del coto.", hunter);
    }

    public override void Exit()
    {
        if (hunter.Navigation != null && hunter.Navigation.IsFollowingPath)
            hunter.Navigation.StopFollowingPath();
        hunter.SetBaitPlacementPaused(false);
        isPlacingBait = false;
    }
}