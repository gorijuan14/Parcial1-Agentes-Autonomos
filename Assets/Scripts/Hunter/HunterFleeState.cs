using UnityEngine;
using System.Collections.Generic;

public class HunterFleeState : HunterState
{
    private readonly StateIndicator indicator;
    private float waitTimer;
    private float nextAttempt;
    private bool waiting;
    private struct RefugeRoute
    {
        public ZoneGizmo Zone;
        public List<GraphNode> Path;
        public float Length;
    }
    public HunterFleeState(HunterFSM fsm, Hunter hunter) : base(fsm, hunter)
    {
        indicator = hunter.GetComponentInChildren<StateIndicator>();
    }
    public override void Enter()
    {
        hunter.ClearTarget();
        hunter.AvoidWolvesDuringNavigation = true;
        hunter.EmergencyEvasion = false;
        hunter.SetBaitPlacementPaused(false);
        hunter.SetBehaviour(null);
        hunter.StopMovement();
        waiting = false;
        waitTimer = 0f;
        nextAttempt = 0f;
        hunter.SelectedSafeZone = null;
        indicator.SetHunterFlee();
        ChooseRefuge();
    }
    private void ChooseRefuge()
    {
        hunter.SelectedSafeZone = null;
        float bestLength = float.PositiveInfinity;
        var routes = new List<RefugeRoute>();
        if (hunter.Navigation == null || hunter.SafeZones == null)
            return;
        foreach (ZoneGizmo zone in hunter.SafeZones)
        {
            if (zone == null)
                continue;
            Vector3 destination = zone.Center;
            destination.y = hunter.transform.position.y;
            if (hunter.Navigation.TryPlanPath(destination, out List<GraphNode> path, out float length))
            {
                bestLength = Mathf.Min(bestLength, length);
                routes.Add(new RefugeRoute { Zone = zone, Path = path, Length = length });
            }
        }

        // Only similarly short routes compete on safety; longer detours stay excluded.
        float limit = bestLength * (1f + hunter.FleeRouteTolerance) + 0.001f;
        float bestSafety = float.NegativeInfinity;
        float chosenLength = float.PositiveInfinity;
        IReadOnlyList<Wolf> wolves = hunter.VisibleWolves;
        // Prefer finishing only when the exit route is similarly short to the
        // nearest reachable refuge and the hunter is ahead of visible threats.
        if (hunter.HasCompletedHunting && hunter.ExitZone != null)
        {
            foreach (RefugeRoute route in routes)
            {
                if (route.Zone != hunter.ExitZone || route.Length > limit)
                    continue;
                Vector3 exitPosition = hunter.ExitZone.Center;
                float distance = (hunter.transform.position - exitPosition).sqrMagnitude;
                bool aheadOfWolves = true;
                foreach (Wolf wolf in wolves)
                    if (wolf != null && wolf.isActiveAndEnabled &&
                        (wolf.transform.position - exitPosition).sqrMagnitude <= distance)
                    {
                        aheadOfWolves = false;
                        break;
                    }
                if (aheadOfWolves)
                {
                    hunter.SelectedSafeZone = hunter.ExitZone;
                    return;
                }
            }
        }
        foreach (RefugeRoute route in routes)
        {
            if (route.Length > limit)
                continue;
            float safety = RouteSafety(route.Path, route.Zone.Center, wolves);
            if (safety > bestSafety + 0.001f ||
                (Mathf.Abs(safety - bestSafety) <= 0.001f && route.Length < chosenLength))
            {
                bestSafety = safety;
                chosenLength = route.Length;
                hunter.SelectedSafeZone = route.Zone;
            }
        }
    }

    private float RouteSafety(List<GraphNode> path, Vector3 destination, IReadOnlyList<Wolf> wolves)
    {
        if (wolves.Count == 0)
            return 0f;
        int first = 0;
        // Match the initial visible-node shortcut taken by AStarMovement.
        while (first + 1 < path.Count && hunter.CanMoveDirectlyTo(path[first + 1].Position))
            first++;

        float worstSafety = float.PositiveInfinity;
        foreach (Wolf wolf in wolves)
        {
            if (wolf == null || !wolf.isActiveAndEnabled)
                continue;
            Vector3 threat = wolf.transform.position;
            Vector3 previous = hunter.transform.position;
            threat.y = previous.y;
            float clearance = Vector3.Distance(previous, threat);
            float remaining = 8f;
            for (int i = first; i <= path.Count && remaining > 0.001f; i++)
            {
                Vector3 next = i < path.Count ? path[i].Position : destination;
                next.y = previous.y;
                Vector3 offset = next - previous;
                float distance = offset.magnitude;
                if (distance < 0.001f)
                    continue;
                next = previous + offset.normalized * Mathf.Min(distance, remaining);
                Vector3 segment = next - previous;
                float projection = Mathf.Clamp01(Vector3.Dot(threat - previous, segment) / segment.sqrMagnitude);
                clearance = Mathf.Min(clearance, Vector3.Distance(threat, previous + segment * projection));
                remaining -= Vector3.Distance(previous, next);
                previous = next;
            }
            // Penalize passing close to a visible wolf; favor gaining separation too.
            float safety = clearance + Vector3.Distance(previous, threat) * 0.25f;
            worstSafety = Mathf.Min(worstSafety, safety);
        }
        return float.IsPositiveInfinity(worstSafety) ? 0f : worstSafety;
    }
    public override void Update()
    {
        if (hunter.HasCompletedHunting && hunter.ExitZone != null &&
            hunter.ExitZone.Contains(hunter.transform.position, 0.55f))
        {
            hunterFSM.ChangeState(HunterStates.Exit);
            return;
        }
        ZoneGizmo zone = hunter.SelectedSafeZone;
        if (zone != null && zone.Contains(hunter.transform.position, 0.55f))
        {
            if (!waiting)
            {
                hunter.Navigation.StopFollowingPath();
                hunter.EmergencyEvasion = false;
                waiting = true;
                waitTimer = 0f;
            }
            hunter.StopMovement();
            waitTimer += Time.deltaTime;
            if (waitTimer >= hunter.SafeZoneWaitTime)
                hunterFSM.ChangeState(hunter.HasCompletedHunting ? HunterStates.Exit : HunterStates.Search);
            return;
        }
        if (Time.time < nextAttempt)
            return;
        // Wolves move: replan frequently and exclude threatened nodes/edges, even on an active route.
        ChooseRefuge();
        zone = hunter.SelectedSafeZone;
        bool hasSafeRoute = false;
        if (zone != null)
        {
            Vector3 destination = zone.Center;
            destination.y = hunter.transform.position.y;
            hasSafeRoute = hunter.Navigation.TryFollowPosition(destination);
            if (!hasSafeRoute)
                hunter.SelectedSafeZone = null;
        }
        hunter.EmergencyEvasion = !hasSafeRoute;
        if (!hasSafeRoute)
        {
            hunter.Navigation?.StopFollowingPath();
            hunter.SetBehaviour(null);
        }
        nextAttempt = Time.time + 0.4f;
    }
    public override void Exit()
    {
        hunter.AvoidWolvesDuringNavigation = false;
        hunter.EmergencyEvasion = false;
        if (hunter.Navigation != null && hunter.Navigation.IsFollowingPath)
            hunter.Navigation.StopFollowingPath();
        hunter.SelectedSafeZone = null;
        hunter.SetBehaviour(null);
        hunter.StopMovement();
    }
}
