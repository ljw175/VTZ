using UnityEngine;
using System;

public enum EnemyState { Patrol, Chase, Attack }

public class Enemy : MonoBehaviour
{
    [SerializeField] protected float speed;
    [SerializeField] protected float increaseSpeed;
    [SerializeField] protected float Hp;
    [SerializeField] private GameObject destroyParticle; // 사망 시 사용할 큰 폭발
    
    [Tooltip("피격 시 사용할 작은 파티클 (배의 자식 오브젝트로 미리 넣어두세요)")]
    [SerializeField] private ParticleSystem hitParticleSystem; 
    
    [Header("Physics & AI Settings")]
    public float detectionRadius = 15f;
    [SerializeField] protected float turnSpeed = 1.0f;
    [SerializeField] protected float avoidanceRadius = 3.0f;
    [SerializeField] protected float avoidanceForce = 2.0f;
    [SerializeField] protected LayerMask enemyLayerMask = ~0; 
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Planning Phase Intel")]
    [SerializeField] public GameObject intelVisual;
    [SerializeField] protected GameObject shipVisual;

    [Header("Detection Scaling")]
    [SerializeField] protected float detectionGrowthPerSecond = 0.5f; 
    [SerializeField] protected float maxDetectionRadius = 40f;

    public float MaxHp { get; private set; } 
    public event Action<float, float> OnHpChanged; 

    protected Transform target;
    protected Collider2D col;
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 1.5f; 
        
        MaxHp = Hp; 
    }

    protected virtual void Start() { }

    protected bool IsTargetDetectable()
    {
        if (target == null) return false;
        var ship = target.GetComponent<ShipController>();
        return ship == null || !ship.IsDockedAtPort;
    }

    protected virtual void Update()
{
    if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
    {
        if (intelVisual != null) intelVisual.SetActive(true);
        if (shipVisual != null) shipVisual.SetActive(false);
    }
    else 
    {
        if (intelVisual != null) intelVisual.SetActive(false);
        if (shipVisual != null) shipVisual.SetActive(true);

        // 시간 비례 탐지 범위 증가 로직 추가
        detectionRadius = Mathf.Min(maxDetectionRadius, detectionRadius + detectionGrowthPerSecond * Time.deltaTime);
    }
}

    protected void MoveTowardsWithPhysics(Vector2 targetPos, float moveForce)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) return;

        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, avoidanceRadius, enemyLayerMask);
        Vector2 avoidanceVector = Vector2.zero;
        int avoidCount = 0;

        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider.gameObject == this.gameObject) continue;
            
            if (collider.GetComponent<Enemy>() != null || collider.GetComponent<ObstacleContact>() != null)
            {
                Vector2 awayFromEnemy = (Vector2)transform.position - (Vector2)collider.transform.position;
                float dist = awayFromEnemy.magnitude;
                if (dist < 0.1f) dist = 0.1f; 

                avoidanceVector += (awayFromEnemy.normalized / dist);
                avoidCount++;
            }
        }

        if (avoidCount > 0)
        {
            avoidanceVector /= avoidCount;
            dir = (dir + avoidanceVector * avoidanceForce).normalized;
        }

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float currentAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, turnSpeed * 100f * Time.fixedDeltaTime);
        rb.MoveRotation(currentAngle);

        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(rb.rotation, targetAngle));
        if (angleDiff < 60f && moveForce > 0f)
        {
            rb.AddForce(transform.right * moveForce);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
{
    AttackData attack;
    float beforeHp = Hp;
    if (collision.TryGetComponent(out attack))
    {
        // 플레이어의 공격이 아닌 경우 (적의 공격인 경우) 팀킬 방지 및 자폭 방지
        if (!attack.IsPlayerAttack) return;

        Hp -= attack.Damage;
        OnHpChanged?.Invoke(Hp, MaxHp);

            if (Hp <= 0)
            {
                if (WaveManager.Instance != null) WaveManager.Instance.OnEnemyKilled();

                if (TryGetComponent<BossEnemy>(out BossEnemy boss)) AudioManager.Instance.PlayBossDeath(); 
                else AudioManager.Instance.PlayEnemyDeath2(); 

                // 사망 시에만 Instantiate 사용 (큰 폭발)
                Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, -1f);
                var particle = Instantiate(destroyParticle, spawnPos, Quaternion.identity);
                
                ParticleSystem ps = particle.GetComponent<ParticleSystem>();
                var main = ps.main;
                Color particleColor = spriteRenderer.color;
                particleColor.a = 1f;
                main.startColor = particleColor;
                ps.Play();

                Destroy(gameObject);
            }
            else if (beforeHp > Hp)
            {
                // [수정완료] 무분별한 Instantiate를 제거하고 Emit 방식으로 성능 최적화 및 타격감 차등 부여
                if (hitParticleSystem != null)
                {
                    // 입은 데미지에 비례하여 파티클 입자 방출
                    hitParticleSystem.Emit(attack.Damage * 5);
                }
            }
        }
    }
}