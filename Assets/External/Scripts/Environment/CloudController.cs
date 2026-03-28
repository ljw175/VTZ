using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudController : MonoBehaviour
{
    [SerializeField] private float speed = 2.0f;
    [SerializeField] private float colorTransitionDuration = 2f;
    private Vector3 moveDirection;
    private float boundsX = 510f;
    private float boundsY = 510f;

    // --- Weather State ---
    private WeatherStateDefinition currentWeatherState;
    public WeatherStateDefinition CurrentWeatherState => currentWeatherState;
    public bool IsTransitioning { get; private set; }

    private SpriteRenderer[] spriteRenderers;
    private CircleCollider2D effectZone;
    private float lifetimeTimer;
    private bool hasLifetime;

    // --- Effect Particle ---
    private ParticleSystem activeParticle;

    // --- Color Transition ---
    private Coroutine colorTransitionCoroutine;

    // --- DOT Tracking ---
    private HashSet<ShipController> affectedShips = new HashSet<ShipController>();
    private float dotAccumulator = 0f;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    public void Initialize(Vector3 dir, float customSpeed)
    {
        moveDirection = dir.normalized;
        speed = customSpeed;
    }

    public void InitializeWeather(WeatherStateDefinition state)
    {
        // 초기화는 즉시 적용 (전환 애니메이션 없음)
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
            colorTransitionCoroutine = null;
            IsTransitioning = false;
        }

        currentWeatherState = state;
        if (state == null) return;

        speed = state.cloudMoveSpeed;

        SetAllSpritesColor(state.cloudColor);
        Debug.Log($"[Cloud Color] '{gameObject.name}' InitializeWeather → color set to {state.cloudColor} (state: {state.stateName})");

        EnsureEffectZone();
        effectZone.radius = state.effectRadius;

        hasLifetime = state.cloudLifetime >= 0;
        lifetimeTimer = state.cloudLifetime;

        SetupEffectParticle(state);
        ApplyWeatherModifiersToAll();
    }

    public void TransitionWeather(WeatherStateDefinition newState)
    {
        if (newState == null || newState == currentWeatherState) return;

        // 전환 중 새 전환 요청 시 기존 코루틴 중단
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
            colorTransitionCoroutine = null;
        }

        // 1단계: 즉시 — 이전 효과 제거 + 새 상태 저장
        RemoveWeatherModifiersFromAll();

        var previousState = currentWeatherState;
        currentWeatherState = newState;
        speed = newState.cloudMoveSpeed;

        EnsureEffectZone();
        effectZone.radius = newState.effectRadius;

        hasLifetime = newState.cloudLifetime >= 0;
        lifetimeTimer = newState.cloudLifetime;

        // 2단계: 점진적 색상 전환 → 완료 후 파티클/효과 적용
        Color startColor = (spriteRenderers.Length > 0) ? spriteRenderers[0].color : Color.white;
        colorTransitionCoroutine = StartCoroutine(TransitionColorCoroutine(startColor, newState));

        Debug.Log($"[Cloud Color] '{gameObject.name}' TransitionWeather → lerping to {newState.cloudColor} (state: {previousState.stateName} → {newState.stateName})");
    }

    private IEnumerator TransitionColorCoroutine(Color fromColor, WeatherStateDefinition targetState)
    {
        IsTransitioning = true;
        Color toColor = targetState.cloudColor;
        float elapsed = 0f;
        float duration = colorTransitionDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color current = Color.Lerp(fromColor, toColor, t);
            SetAllSpritesColor(current);
            yield return null;
        }

        SetAllSpritesColor(toColor);
        Debug.Log($"[Cloud Color] '{gameObject.name}' color transition complete → {toColor} (state: {targetState.stateName})");

        // 3단계: 색상 완료 → 파티클 적용
        SetupEffectParticle(targetState);

        // 4단계: 파티클 후 → 효과 적용
        ApplyWeatherModifiersToAll();

        IsTransitioning = false;
        colorTransitionCoroutine = null;
    }

    private void SetAllSpritesColor(Color color)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = color;
        }
    }

    private void SetupEffectParticle(WeatherStateDefinition state)
    {
        // 이전 파티클: 방출 중단 → 잔여 파티클 소멸 후 자동 파괴
        if (activeParticle != null)
        {
            FadeOutParticle(activeParticle);
            activeParticle = null;
        }

        if (state.effectParticle == null)
        {
            Debug.Log($"[Effect Particle] '{gameObject.name}' — effectParticle is null for state '{state.stateName}', skipping");
            return;
        }

        // 새 파티클: 즉시 인스턴스화 + 재생
        Debug.Log($"[Effect Particle] '{gameObject.name}' — crossfade: instantiating particle '{state.effectParticle.name}' (radius: {state.effectRadius}, state: {state.stateName})");
        activeParticle = Instantiate(state.effectParticle, transform);
        activeParticle.transform.localPosition = Vector3.zero;

        var shape = activeParticle.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = state.effectRadius;

        if (!activeParticle.isPlaying)
            activeParticle.Play();
    }

    private void FadeOutParticle(ParticleSystem particle)
    {
        Debug.Log($"[Effect Particle] '{gameObject.name}' — fading out particle '{particle.name}'");

        // 새 방출 중단, 기존 파티클은 수명까지 유지
        particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 구름 자식에서 분리하여 구름 파괴/재배치에 영향받지 않도록 함
        particle.transform.SetParent(null);

        // 잔여 파티클 최대 수명 후 오브젝트 파괴
        float remainingLife = particle.main.startLifetime.constantMax;
        StartCoroutine(DestroyAfterParticlesDie(particle, remainingLife));
    }

    private IEnumerator DestroyAfterParticlesDie(ParticleSystem particle, float maxLifetime)
    {
        yield return new WaitForSeconds(maxLifetime);

        if (particle != null)
            Destroy(particle.gameObject);
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

        // 전환 중이면 효과 적용을 보류 (전환 완료 시 일괄 적용)
        if (!IsTransitioning)
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

    private void ApplyWeatherModifiersToAll()
    {
        foreach (var ship in affectedShips)
        {
            if (ship != null && ship.RuntimeState != null)
                ApplyWeatherModifiers(ship);
        }
    }

    private void RemoveWeatherModifiersFromAll()
    {
        foreach (var ship in affectedShips)
        {
            if (ship != null && ship.RuntimeState != null)
                ship.RuntimeState.RemoveEnvironmentalModifiers(this);
        }
    }

    private void OnDestroy()
    {
        if (activeParticle != null)
            Destroy(activeParticle.gameObject);

        // 파괴 시 영향받던 선박들의 수정자 제거
        foreach (var ship in affectedShips)
        {
            if (ship != null && ship.RuntimeState != null)
                ship.RuntimeState.RemoveEnvironmentalModifiers(this);
        }
        affectedShips.Clear();
    }
}
