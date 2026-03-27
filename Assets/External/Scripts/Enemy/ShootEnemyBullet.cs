using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public class ShootEnemyBullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float speed = 5f;
    public int damage = 1;
    public float lifetime = 5f;

    private Vector2 savedVelocity;
    private Transform shooterTransform;
    private Vector2 direction;
    private Rigidbody2D rb;
    private PolygonCollider2D col;
    private bool isPaused = false;
    private bool isReflected = false;

    public void Initialize(Vector2 dir, float bulletSpeed, int bulletDamage, Transform shooter)
    {
        direction = dir.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        shooterTransform = shooter;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed; 
        }

        // 발사체의 기본 데미지 설정 (Enemy.cs 등에서 이 AttackData를 읽어 데미지를 입음)
        AttackData attackData = GetComponent<AttackData>();
        if (attackData == null) attackData = gameObject.AddComponent<AttackData>();
        attackData.Damage = damage;

        StartCoroutine(DestroyAfterTime());
    }

    private void Update()
    {
        if (rb == null) return;
        
        bool currentlyPaused = (GameManager.Instance.CurrentPhase == GamePhase.Paused);

        if (currentlyPaused && !isPaused)
        {
            savedVelocity = rb.linearVelocity; 
            rb.linearVelocity = Vector2.zero; 
            rb.angularVelocity = 0f; 
            
            isPaused = true;
        }
        else if (!currentlyPaused && isPaused)
        {
            rb.linearVelocity = savedVelocity; 
            isPaused = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) 
    {
        // 1. 벽이나 섬에 부딪히면 소멸
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Island"))
        {
            Destroy(gameObject);
            return;
        }

        // 2. 플레이어와 충돌 시
        if (collision.gameObject.CompareTag("Player"))
        {
            ShipController playerShip = collision.GetComponent<ShipController>();
            if (playerShip != null) 
            {
                // [수정] 하드코딩된 내부 damage 변수 대신, CharacterShoot이 주입한 AttackData의 데미지를 우선 적용
                AttackData attackData = GetComponent<AttackData>();
                int finalDamage = attackData != null ? attackData.Damage : damage;
                
                playerShip.TakeDamage(finalDamage); 
            }

            // 실제 플레이어 본체에 부딪힌 경우 (발사자가 플레이어 본인인 경우 팀킬 방지)
            if (shooterTransform != null && shooterTransform.CompareTag("Player") && !isReflected) return;

            Destroy(gameObject);
            return;
        }

        // 3. 적과 충돌 시
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 방금 쏜 발사자 본인과의 즉시 충돌 방지 및 적끼리의 팀킬 방지 (반사된 총알은 예외)
            if (shooterTransform != null && !isReflected)
            {
                if (collision.gameObject == shooterTransform.gameObject || shooterTransform.CompareTag("Enemy"))
                {
                    return;
                }
            }

            // 다른 경우(플레이어가 쏜 총알이거나 반사된 총알)에는 적을 맞추고 파괴
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        return CoroutineHelper.DestroyAfterPausedTime(gameObject, lifetime);
    }
}
