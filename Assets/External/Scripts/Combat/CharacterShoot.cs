using UnityEngine;

public class CharacterShoot : MonoBehaviour
{
    [Header("사격 설정")]
    [Tooltip("발사할 포탄 프리팹")]
    [SerializeField] private GameObject bulletPrefab;
    
    [Tooltip("포탄이 생성될 위치들 (배열로 두어 다중 포문 지원)")]
    [SerializeField] private Transform[] firePoints;
    
    [Tooltip("연사 쿨타임 (초)")]
    [SerializeField] private float fireCooldown = 0.5f;
    
    [Tooltip("포탄의 날아가는 속도")]
    [SerializeField] private float bulletSpeed = 15f;
    
    [Tooltip("포탄의 데미지")]
    [SerializeField] private int damage = 10;

    [Header("오디오 설정")]
    [SerializeField] private bool isPlayer = true;

    private float currentCooldown = 0f;

    private void Update()
    {
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 외부 컨트롤러(Player/Enemy)에서 계산된 각도를 받아 발사를 시도합니다.
    /// </summary>
    /// <param name="fireAngle">발사할 절대 각도 (도 단위)</param>
    /// <returns>발사 성공 여부 (쿨타임 체크용)</returns>
    public bool TryShoot(float fireAngle)
    {
        if (currentCooldown > 0f || bulletPrefab == null || firePoints.Length == 0)
        {
            return false; // 쿨타임 중이거나 세팅이 안 됨
        }

        // 각도를 방향 벡터로 변환
        float angleRad = fireAngle * Mathf.Deg2Rad;
        Vector2 fireDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;

        foreach (Transform firePoint in firePoints)
        {
            // 포탄 생성 (Z축 회전을 적용하여 생성)
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0f, 0f, fireAngle));

            // 1. 물리적 이동 (공용 처리 - ShootEnemyBullet.cs의 Initialize 의존성 제거)
            Rigidbody2D rb = bulletObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = fireDirection * bulletSpeed;
            }

            // 2. 데미지 세팅 부분 수정
            AttackData attackData = bulletObj.GetComponent<AttackData>();
            if (attackData == null)
            {
                attackData = bulletObj.AddComponent<AttackData>();
            }
            attackData.Damage = damage;
            attackData.IsPlayerAttack = isPlayer; // 발사 주체가 누구인지 총알에 각인
            
            // 3. 적 탄막일 경우, 플레이어와 충돌 시 넉백 기준점이 되도록 슈터(자신)를 전달할 수 있으나
            // MVP 최적화를 위해 발사 방향의 반대 방향으로 넉백되도록 총알 스크립트를 개조하는 것이 유리합니다.
        }

        // 효과음 재생
        if (AudioManager.Instance != null)
        {
            if (isPlayer) 
                AudioManager.Instance.PlayEpicMobShoot(); // 임시로 에픽몹 효과음 공유 (추후 플레이어 전용 사운드 교체)
            else 
                AudioManager.Instance.PlayEpicMobShoot();
        }

        currentCooldown = fireCooldown;
        return true;
    }
}