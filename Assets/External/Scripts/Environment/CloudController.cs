using System.Collections.Generic;
using UnityEngine;

public class CloudController : MonoBehaviour
{
    [SerializeField] private float speed = 2.0f;
    private Vector3 moveDirection;
    private float boundsX = 510f;
    private float boundsY = 510f;

    // --- Weather State ---
    private WeatherStateDefinition currentWeatherState;
    public WeatherStateDefinition CurrentWeatherState => currentWeatherState;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D effectZone;
    private float lifetimeTimer;
    private bool hasLifetime;

    // --- DOT Tracking ---
    private HashSet<ShipController> affectedShips = new HashSet<ShipController>();
    private float dotAccumulator = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(Vector3 dir, float customSpeed)
    {
        moveDirection = dir.normalized;
        speed = customSpeed;
    }

    public void InitializeWeather(WeatherStateDefinition state)
    {
        currentWeatherState = state;
        if (state == null) return;

        speed = state.cloudMoveSpeed;

        if (spriteRenderer != null)
            spriteRenderer.color = state.cloudColor;

        EnsureEffectZone();
        effectZone.radius = state.effectRadius;

        hasLifetime = state.cloudLifetime >= 0;
        lifetimeTimer = state.cloudLifetime;
    }

    public void TransitionWeather(WeatherStateDefinition newState)
    {
        if (newState == null || newState == currentWeatherState) return;

        // 기존 영향받던 선박들의 이전 수정자 제거
        foreach (var ship in affectedShips)
        {
            if (ship != null && ship.RuntimeState != null)
                ship.RuntimeState.RemoveEnvironmentalModifiers(this);
        }

        currentWeatherState = newState;
        speed = newState.cloudMoveSpeed;

        if (spriteRenderer != null)
            spriteRenderer.color = newState.cloudColor;

        EnsureEffectZone();
        effectZone.radius = newState.effectRadius;

        hasLifetime = newState.cloudLifetime >= 0;
        lifetimeTimer = newState.cloudLifetime;

        // 현재 영역 내 선박에 새 수정자 적용
        foreach (var ship in affectedShips)
        {
            if (ship != null && ship.RuntimeState != null)
                ApplyWeatherModifiers(ship);
        }
    }

    private void EnsureEffectZone()
    {
        if (effectZone == null)
        {
            effectZone = GetComponent<CircleCollider2D>();
            if (effectZone == null)
                effectZone = gameObject.AddComponent<CircleCollider2D>();
            effectZone.isTrigger = true;
        }
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;

        // Lifetime countdown
        if (hasLifetime && currentWeatherState != null)
        {
            lifetimeTimer -= Time.deltaTime;
            if (lifetimeTimer <= 0f)
            {
                if (WeatherManager.Instance != null)
                    WeatherManager.Instance.RepositionCloud(this);
                return;
            }
        }

        // DOT damage
        if (currentWeatherState != null && currentWeatherState.hullDamagePerSecond > 0f && affectedShips.Count > 0)
        {
            dotAccumulator += currentWeatherState.hullDamagePerSecond * Time.deltaTime;
            if (dotAccumulator >= 1f)
            {
                int damage = Mathf.FloorToInt(dotAccumulator);
                dotAccumulator -= damage;

                foreach (var ship in affectedShips)
                {
                    if (ship != null)
                        ship.TakeDamage(damage);
                }
            }
        }

        CheckOutOfBounds();
    }

    private void CheckOutOfBounds()
    {
        if (Mathf.Abs(transform.position.x) > boundsX || Mathf.Abs(transform.position.y) > boundsY)
        {
            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.RepositionCloud(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var ship = other.GetComponent<ShipController>();
        if (ship == null || ship.RuntimeState == null) return;

        affectedShips.Add(ship);
        ApplyWeatherModifiers(ship);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var ship = other.GetComponent<ShipController>();
        if (ship == null || ship.RuntimeState == null) return;

        affectedShips.Remove(ship);
        ship.RuntimeState.RemoveEnvironmentalModifiers(this);
    }

    private void ApplyWeatherModifiers(ShipController ship)
    {
        if (currentWeatherState == null || currentWeatherState.statEffects == null || currentWeatherState.statEffects.Length == 0)
            return;

        var modifiers = new List<StatModifier>(currentWeatherState.statEffects);
        ship.RuntimeState.AddEnvironmentalModifiers(this, modifiers);
    }

    private void OnDestroy()
    {
        // 파괴 시 영향받던 선박들의 수정자 제거
        foreach (var ship in affectedShips)
        {
            if (ship != null && ship.RuntimeState != null)
                ship.RuntimeState.RemoveEnvironmentalModifiers(this);
        }
        affectedShips.Clear();
    }
}
