using UnityEngine;
using System.Collections;

public class BossBullet : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private int maxBounce = 2;
    [SerializeField] private int damage = 5;
    [SerializeField] private float lifetime = 4f;

    private Rigidbody2D rb;
    private PolygonCollider2D col;
    private int bounceCount = 0;
    private Vector2 moveDir;
    private Transform shooterTransform;
    private Vector2 direction;

    private Vector2 savedVelocity;
    private bool isPaused = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<PolygonCollider2D>();
    }
    void Start()
    {
        // 처음 발사 방향 (총구 방향 기준)
        moveDir = transform.up.normalized;
    }

    void Update()
    {
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

        StartCoroutine(DestroyAfterTime());
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Wall"))
        {
            bounceCount++;

            if (bounceCount > maxBounce)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 inDirection = rb.linearVelocity.normalized;
            Vector2 prevPosition = (Vector2)transform.position - (rb.linearVelocity * Time.fixedDeltaTime);
            Vector2 closestPoint = collision.ClosestPoint(prevPosition);
            Vector2 normal = (closestPoint - prevPosition).normalized;

            moveDir = Vector2.Reflect(inDirection, normal).normalized;

            rb.linearVelocity = moveDir * speed;
            
            return;
        }

        Vector2 knockbackDirection = Vector2.zero;
        AttackData attack;
        if (collision.TryGetComponent(out attack) && collision.gameObject.CompareTag("Player"))
        {
            if (shooterTransform != null)
            {
                knockbackDirection = (shooterTransform.position - transform.position).normalized;
            }
            else
            {
                knockbackDirection = -rb.linearVelocity.normalized;
            }

            rb.linearVelocity = knockbackDirection * speed * 1.5f;
            col.isTrigger = true;

            float angle = Mathf.Atan2(knockbackDirection.y, knockbackDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            AttackData attackData = gameObject.GetComponent<AttackData>();
            if (attackData != null) attackData.Damage = damage;

            return;
        }   
    }

    private IEnumerator DestroyAfterTime()
    {
        return CoroutineHelper.DestroyAfterPausedTime(gameObject, lifetime);
    }
}