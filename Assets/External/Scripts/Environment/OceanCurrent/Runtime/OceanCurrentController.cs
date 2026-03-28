using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class OceanCurrentController : MonoBehaviour
{
    [SerializeField] private OceanCurrentDefinition definition;
    [SerializeField] private Color activeColor = new Color(0.2f, 0.5f, 1f, 0.5f);
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);

    private LineRenderer lineRenderer;
    private Vector3[] pathPoints;
    private bool isActive = false;
    private HashSet<ShipController> affectedShips = new HashSet<ShipController>();
    private ShipController cachedPlayer;

    public bool IsActive => isActive;
    public OceanCurrentDefinition Definition => definition;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        CachePathPoints();
    }

    private void OnEnable()
    {
        if (OceanCurrentManager.Instance != null)
            OceanCurrentManager.Instance.RegisterCurrent(this);
    }

    private void OnDisable()
    {
        if (OceanCurrentManager.Instance != null)
            OceanCurrentManager.Instance.UnregisterCurrent(this);
    }

    private void CachePathPoints()
    {
        if (lineRenderer == null || lineRenderer.positionCount < 2)
        {
            pathPoints = null;
            return;
        }

        pathPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(pathPoints);
    }

    public void SetActiveState(bool active)
    {
        isActive = active;
        lineRenderer.startColor = active ? activeColor : inactiveColor;
        lineRenderer.endColor = active ? activeColor : inactiveColor;

        if (!active)
        {
            foreach (var ship in affectedShips)
            {
                if (ship != null && ship.RuntimeState != null)
                    ship.RuntimeState.RemoveEnvironmentalModifiers(this);
            }
            affectedShips.Clear();
        }
    }

    private void FixedUpdate()
    {
        if (!isActive || pathPoints == null || pathPoints.Length < 2) return;

        ShipController ship = FindPlayerShip();
        if (ship == null) return;

        if (TryGetNearestProjection((Vector2)ship.transform.position, out Vector2 nearestPoint, out int segmentIndex))
        {
            // 선박이 해류 영역 안에 있음
            if (!affectedShips.Contains(ship))
            {
                affectedShips.Add(ship);
                ApplyFrictionModifier(ship);
            }

            // 해류 방향으로 힘 적용
            Vector2 forceDir = GetSegmentDirection(segmentIndex);
            if (definition.reverseDirection) forceDir = -forceDir;

            Rigidbody2D rb = ship.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.AddForce(forceDir * definition.forceStrength, ForceMode2D.Force);
        }
        else
        {
            // 선박이 해류 영역 밖
            if (affectedShips.Contains(ship))
            {
                affectedShips.Remove(ship);
                if (ship.RuntimeState != null)
                    ship.RuntimeState.RemoveEnvironmentalModifiers(this);
            }
        }
    }

    /// <summary>
    /// ShipController.TryGetSlipstreamData()와 동일한 point-to-line-segment projection 알고리즘.
    /// LineRenderer 경로점과 definition.detectionRadius를 사용.
    /// </summary>
    private bool TryGetNearestProjection(Vector2 position, out Vector2 nearestPoint, out int segmentIndex)
    {
        nearestPoint = Vector2.zero;
        segmentIndex = -1;

        float minDistSqr = float.MaxValue;
        bool found = false;
        float radiusSqr = definition.detectionRadius * definition.detectionRadius;

        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector2 a = pathPoints[i];
            Vector2 b = pathPoints[i + 1];

            if ((b - a).sqrMagnitude < 0.001f) continue;

            Vector2 ab = b - a;
            float t = Vector2.Dot(position - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 projection = a + t * ab;

            float distSqr = (position - projection).sqrMagnitude;
            if (distSqr < radiusSqr && distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                nearestPoint = projection;
                segmentIndex = i;
                found = true;
            }
        }

        return found;
    }

    private Vector2 GetSegmentDirection(int segmentIndex)
    {
        return ((Vector2)pathPoints[segmentIndex + 1] - (Vector2)pathPoints[segmentIndex]).normalized;
    }

    private void ApplyFrictionModifier(ShipController ship)
    {
        if (ship.RuntimeState == null) return;

        var modifiers = new List<StatModifier>
        {
            new StatModifier(ShipStatType.WaterFriction, StatModifierType.Flat, definition.waterFrictionModifier)
        };
        ship.RuntimeState.AddEnvironmentalModifiers(this, modifiers);
    }

    private ShipController FindPlayerShip()
    {
        if (cachedPlayer != null) return cachedPlayer;
        cachedPlayer = FindFirstObjectByType<ShipController>();
        return cachedPlayer;
    }

    private void OnDestroy()
    {
        foreach (var ship in affectedShips)
        {
            if (ship != null && ship.RuntimeState != null)
                ship.RuntimeState.RemoveEnvironmentalModifiers(this);
        }
        affectedShips.Clear();
    }
}
