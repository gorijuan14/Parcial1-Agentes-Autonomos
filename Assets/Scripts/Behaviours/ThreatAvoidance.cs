using System;
using System.Collections.Generic;
using UnityEngine;

public static class ThreatAvoidance
{
    public static bool BlocksTravel(Vector3 position, Vector3 direction, Vector3 threat,
        float lookAhead, float corridorWidth)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return false;
        direction.Normalize();
        Vector3 offset = threat - position;
        offset.y = 0f;
        float forward = Vector3.Dot(offset, direction);
        return forward > 0f && forward <= lookAhead &&
            (offset - direction * forward).sqrMagnitude <= corridorWidth * corridorWidth;
    }

    public static bool IsSegmentSafe(Vector3 from, Vector3 to, Vector3 threat, float radius)
    {
        from.y = to.y = threat.y = 0f;
        Vector3 offset = to - from;
        Vector3 away = from - threat;
        if (offset.sqrMagnitude < 0.000001f)
            return away.sqrMagnitude >= radius * radius;

        if (away.sqrMagnitude < radius * radius)
        {
            // An agent already in danger must be allowed to leave, never move closer.
            return Vector3.Dot(away, offset) >= 0f && (to - threat).sqrMagnitude > away.sqrMagnitude;
        }
        float projection = Mathf.Clamp01(Vector3.Dot(threat - from, offset) / offset.sqrMagnitude);
        return (from + offset * projection - threat).sqrMagnitude >= radius * radius;
    }

    private static bool IsSafe(Vector3 from, Vector3 to, IReadOnlyList<Vector3> threats, float radius)
    {
        foreach (Vector3 threat in threats)
            if (!IsSegmentSafe(from, to, threat, radius))
                return false;
        return true;
    }

    public static Vector3 SafeVelocity(Vector3 position, Vector3 proposed, IReadOnlyList<Vector3> threats,
        float radius, float horizon, float maxSpeed, bool emergency,
        Func<Vector3, Vector3, bool> clearOfWalls)
    {
        proposed.y = 0f;
        if (threats.Count == 0 || (!emergency && IsSafe(position, position + proposed * horizon, threats, radius)))
            return proposed;

        float speed = emergency || proposed.magnitude < 0.1f ? maxSpeed : proposed.magnitude;
        Vector3 preferred = proposed.normalized;
        if (emergency || preferred.sqrMagnitude < 0.001f)
        {
            preferred = Vector3.zero;
            foreach (Vector3 threat in threats)
            {
                Vector3 away = position - threat;
                away.y = 0f;
                preferred += away / Mathf.Max(0.1f, away.sqrMagnitude);
            }
            preferred.Normalize();
        }

        Vector3 best = Vector3.zero;
        float bestScore = float.NegativeInfinity;
        for (int i = -1; i < 32; i++)
        {
            float angle = Mathf.PI * 2f * Mathf.Max(0, i) / 32f;
            Vector3 direction = i == -1 ? preferred : new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            if (direction.sqrMagnitude < 0.001f)
                continue;
            Vector3 candidate = direction * speed;
            Vector3 end = position + candidate * horizon;
            if (!IsSafe(position, end, threats, radius) || !clearOfWalls(position, end))
                continue;

            float separation = float.PositiveInfinity;
            foreach (Vector3 threat in threats)
            {
                Vector3 offset = end - threat;
                offset.y = 0f;
                separation = Mathf.Min(separation, offset.magnitude);
            }
            float score = separation + Vector3.Dot(direction, preferred) * 0.5f;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        // If every direction is blocked, braking is safer than following a suicidal route.
        return best;
    }
}
