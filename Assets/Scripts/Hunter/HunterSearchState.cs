using System.Collections.Generic;
using UnityEngine;

public class HunterSearchState : HunterState
{
    private readonly HunterSensor sensor;
    private readonly StateIndicator indicator;
    private readonly Pursue pursue;
    private bool isPlacingBait;
    private float baitPlacementTimer;
    private float nextRouteAttempt;
    private bool travellingToHuntingZone;
    private float timeWithoutBoids;

    public HunterSearchState(HunterFSM fsm, Hunter hunter) : base(fsm, hunter)
    {
        sensor = hunter.GetComponentInChildren<HunterSensor>();
        indicator = hunter.GetComponentInChildren<StateIndicator>();
        pursue = hunter.GetComponent<Pursue>();
    }

    public override void Enter()
    {
        isPlacingBait = false;
        baitPlacementTimer = 0f;
        nextRouteAttempt = 0f;
        travellingToHuntingZone = false;
        timeWithoutBoids = 0f;
        hunter.SetBaitPlacementPaused(false);
        hunter.SetBehaviour(null);
        if (hunter.CurrentTarget == null)
            hunter.StopMovement();
        indicator.SetHunterSearch();
    }

    public override void Update()
    {
        if (hunter.HasCompletedHunting)
        {
            hunterFSM.ChangeState(HunterStates.Exit);
            return;
        }
        AStarMovement navigation = hunter.Navigation;
        if (TryEnterGather(sensor))
            return;

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

        bool baitReady = hunter.UpdateBaits();
        // Search transitions have priority over waiting to place another bait.
        Transform previousTarget = hunter.CurrentTarget;
        Boid focusedBoid = previousTarget != null ? previousTarget.GetComponent<Boid>() : null;
        Transform liveBoid = focusedBoid != null && !focusedBoid.IsDead && !focusedBoid.IsCollected &&
            sensor.CanSee(previousTarget) ? previousTarget : sensor.GetClosestBoid();
        if (previousTarget != null && previousTarget != liveBoid)
            previousTarget.GetComponent<Boid>()?.HideHealthBar();
        hunter.SetTarget(liveBoid);
        if (liveBoid != null)
        {
            timeWithoutBoids = 0f;
            // Even during TBA, visible prey cancels bait placement.
            if (isPlacingBait)
            {
                isPlacingBait = false;
                hunter.SetBaitPlacementPaused(false);
                indicator.SetHunterSearch();
            }
        }
        else
        {
            timeWithoutBoids += Time.deltaTime;
        }
        if (hunter.CanAttack && liveBoid != null)
        {
            hunter.SetTarget(liveBoid);
            hunterFSM.ChangeState(HunterStates.Attack);
            return;
        }

        if (liveBoid != null)
        {
            TrackVisibleBoid(liveBoid, navigation);
            return;
        }

        if (isPlacingBait)
        {
            baitPlacementTimer += Time.deltaTime;
            if (baitPlacementTimer >= hunter.BaitPlacementDuration)
            {
                hunter.SpawnBait();
                hunter.SetBaitPlacementPaused(false);
                isPlacingBait = false;
                indicator.SetHunterSearch();
            }
            return;
        }
        if (liveBoid == null && timeWithoutBoids >= hunter.BaitSearchDelay && baitReady)
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

    private void TrackVisibleBoid(Transform target, AStarMovement navigation)
    {
        target.GetComponent<Boid>()?.ShowHealthBar();
        if (hunter.CanMoveDirectlyTo(target.position))
        {
            if (navigation.IsFollowingPath)
                navigation.StopFollowingPath();

            if (Vector3.Distance(hunter.transform.position, target.position) <= hunter.MeleeAttackRadius)
            {
                hunter.SetBehaviour(null);
                hunter.StopMovement();
                hunter.FaceTarget(target);
            }
            else
            {
                pursue.SetTarget(target);
                hunter.SetBehaviour(pursue);
            }
        }
        else if (Time.time >= nextRouteAttempt)
        {
            if (!navigation.TryFollowPosition(target.position))
            {
                target.GetComponent<Boid>()?.HideHealthBar();
                sensor.IgnoreTemporarily(target);
                hunter.ClearTarget();
            }
            nextRouteAttempt = Time.time + 0.75f;
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
        pursue.SetTarget(null);
    }
}
