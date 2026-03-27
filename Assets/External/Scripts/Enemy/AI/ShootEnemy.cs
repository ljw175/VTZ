using UnityEngine;
using System.Collections;

public class ShootEnemy : Enemy
{
    [Header("사격 컴포넌트")]
    [Tooltip("포격 모듈")]
    [SerializeField] private CharacterShoot cannonShooter;
    [Tooltip("측면 사격 제한 각도")] 
    [SerializeField] private float maxCannonDeviation = 40f; 
    [Tooltip("대포 스프라이트")]
    [SerializeField] private GameObject cannonSprite;

    [Header("해상전 물리 설정")]
    [Tooltip("공격 사거리")]
    [SerializeField] private float attackRange = 4f;
    [Tooltip("사격 쿨다운")]
    [SerializeField] private float fireCooldown = 3f;

    private Vector2 patrolTarget;
    private float fireTimer = 0f;
    public float cannonAngle { get; private set; }

    protected override void Start()
    {
        base.Start();
        patrolTarget = transform.position; // 시작 시 제자리 주변을 패트롤 목표로 설정

        if (cannonShooter == null)
        {
            cannonShooter = GetComponentInChildren<CharacterShoot>();
        }
    }

    protected override void Update()
    {
        UpdateCannonAngle();
        base.Update();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null) return;

        float distToPlayer = Vector2.Distance(transform.position, target.position);

        // --- 상태 전환 (FSM) ---
        if (currentState == EnemyState.Patrol)
        {
            if (distToPlayer <= detectionRadius) 
                currentState = EnemyState.Chase;
        }
        else if (currentState == EnemyState.Chase)
        {
            if (distToPlayer > detectionRadius * 1.5f) 
                currentState = EnemyState.Patrol; // 플레이어가 도망가면 다시 패트롤
            else if (distToPlayer <= attackRange && IsTargetInFireAngle(out _)) 
                currentState = EnemyState.Attack; // 사거리 내이고 사격 각도 안이면 공격
        }
        else if (currentState == EnemyState.Attack)
        {
            if (distToPlayer > attackRange * 1.2f || !IsTargetInFireAngle(out _)) 
                currentState = EnemyState.Chase;
        }

        // --- 상태별 행동 실행 ---
        switch (currentState)
        {
            case EnemyState.Patrol:
                ExecutePatrol();
                break;
            case EnemyState.Chase:
                ExecuteChase();
                break;
            case EnemyState.Attack:
                ExecuteAttack();
                break;
        }
    }

    private void ExecutePatrol()
    {
        // 패트롤 목적지에 도착했으면 새로운 무작위 목적지 탐색
        if (Vector2.Distance(transform.position, patrolTarget) < 3f)
        {
            patrolTarget = (Vector2)transform.position + new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f));
        }
        // 패트롤은 순항 속도의 50%로 천천히 이동
        MoveTowardsWithPhysics(patrolTarget, speed * 0.5f);
    }

    private void ExecuteChase()
{
    if (target == null) return;
    
    Vector2 dirToPlayer = target.position - transform.position;
    float distToPlayer = dirToPlayer.magnitude;
    dirToPlayer.Normalize();

    // 1. 거리가 멀면 플레이어를 향해 전속력 직진
    if (distToPlayer > attackRange * 0.9f)
    {
        MoveTowardsWithPhysics(target.position, speed);
    }
    // 2. 사거리 내에 진입하면 평행 이동(Orbiting) 기동 시작
    else
    {
        // 플레이어를 향하는 벡터의 직교 벡터(Tangent)를 구함 (측면을 내어주는 방향)
        Vector2 tangentLeft = new Vector2(-dirToPlayer.y, dirToPlayer.x);
        Vector2 tangentRight = new Vector2(dirToPlayer.y, -dirToPlayer.x);

        // 현재 선수(뱃머리)가 향하는 각도와 더 가까운 직교 벡터를 선택하여 자연스러운 선회 유도
        float currentAngleRad = rb.rotation * Mathf.Deg2Rad;
        Vector2 forwardDir = new Vector2(Mathf.Cos(currentAngleRad), Mathf.Sin(currentAngleRad));

        float dotLeft = Vector2.Dot(forwardDir, tangentLeft);
        float dotRight = Vector2.Dot(forwardDir, tangentRight);

        Vector2 chosenTangent = dotLeft > dotRight ? tangentLeft : tangentRight;

        // 거리가 너무 가까워지면 밀어내는 벡터(회피/척력)를 살짝 섞어 거리를 일정하게 유지
        Vector2 maintainDistanceVector = Vector2.zero;
        if (distToPlayer < attackRange * 0.6f)
        {
            maintainDistanceVector = -dirToPlayer * 0.5f;
        }

        // 최종 이동 벡터 = 평행 이동(공전) + 거리 유지
        Vector2 tacticalMoveVector = (chosenTangent + maintainDistanceVector).normalized;
        Vector2 moveTarget = (Vector2)transform.position + tacticalMoveVector * 5f;
        
        // 측면 기동 시에는 속도를 약간 줄여 안정적인 포격 각도 및 관성 통제
        MoveTowardsWithPhysics(moveTarget, speed * 0.8f);
    }
}

    private void ExecuteAttack()
    {
        // 포격 각도를 유지하며 속도를 줄여 관성에 의해 스르륵 미끄러지며 사격
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 2f);

        fireTimer += Time.fixedDeltaTime;
        if (fireTimer >= fireCooldown)
        {
            float fireAngle = 0f;
            if (IsTargetInFireAngle(out fireAngle) && cannonShooter != null)
            {
                float currentAngle = rb.rotation;
                float leftSide = currentAngle + 90f;
                float rightSide = currentAngle - 90f;

                float diffLeft = Mathf.Abs(Mathf.DeltaAngle(leftSide, fireAngle));
                float diffRight = Mathf.Abs(Mathf.DeltaAngle(rightSide, fireAngle));

                float baseAngle = diffLeft < diffRight ? leftSide : rightSide;

                float delta = Mathf.DeltaAngle(baseAngle, fireAngle);
                delta = Mathf.Clamp(delta, -maxCannonDeviation, maxCannonDeviation);
                
                float finalCannonAngle = baseAngle + delta;

                if (cannonShooter.TryShoot(finalCannonAngle))
                {
                    fireTimer = 0f;
                    // 발사한 대포의 반대 방향으로 선체가 밀리는 반동 적용
                    float rad = finalCannonAngle * Mathf.Deg2Rad;
                    Vector2 fireDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    rb.AddForce(-fireDir * speed * 2f, ForceMode2D.Impulse);
                }
            }
        }
    }

    // 플레이어가 우현 또는 좌현 포격 가능 각도 안에 들어왔는지 검사
    private bool IsTargetInFireAngle(out float targetAngle)
    {
        targetAngle = 0f;
        if (target == null) return false;

        Vector2 dirToPlayer = target.position - transform.position;
        targetAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;

        float currentAngle = rb.rotation;
        float leftSide = currentAngle + 90f;
        float rightSide = currentAngle - 90f;

        float diffLeft = Mathf.Abs(Mathf.DeltaAngle(leftSide, targetAngle));
        float diffRight = Mathf.Abs(Mathf.DeltaAngle(rightSide, targetAngle));

        return diffLeft <= maxCannonDeviation || diffRight <= maxCannonDeviation;
    }

    // ShootEnemy의 포신이 플레이어 위치를 추적하여 각도 조정
    private void UpdateCannonAngle()
    {
        if (target != null || cannonShooter != null)
        {
            Vector2 dirToPlayer = target.position - transform.position;
            float targetAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
            float leftSide = targetAngle + 90f;
            float rightSide = targetAngle - 90f;

            float diffLeft = Mathf.Abs(Mathf.DeltaAngle(rb.rotation, leftSide));
            float diffRight = Mathf.Abs(Mathf.DeltaAngle(rb.rotation, rightSide));

            float baseAngle = diffLeft < diffRight ? leftSide : rightSide;

            float delta = Mathf.Clamp(Mathf.DeltaAngle(baseAngle, targetAngle), -maxCannonDeviation, maxCannonDeviation);
            
            cannonAngle = baseAngle + delta;
            cannonSprite.transform.rotation = Quaternion.Euler(0f, 0f, cannonAngle);
        }
    }
}