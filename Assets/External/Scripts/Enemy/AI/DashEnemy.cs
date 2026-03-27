using UnityEngine;
using System.Collections;

public class DashEnemy : Enemy
{
    [Header("해양 괴물 돌진 설정")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float waitBeforeDash = 0.5f;
    [SerializeField] private float attackRange = 5f;

    private Vector2 patrolTarget;
    private float attackTimer = 0f;
    private bool isDashing = false;

    protected override void Start()
    {
        base.Start();
        patrolTarget = transform.position; // 시작 시 제자리 주변을 패트롤 목표로 설정
        attackTimer = dashCooldown; // 시작 시 바로 대시 가능하게 초기화
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // [수정] 쿨타임 타이머는 상태에 종속되지 않고 항상 흐르도록 밖으로 분리
        if (attackTimer < dashCooldown)
        {
            attackTimer += Time.fixedDeltaTime;
        }

        if (target == null || isDashing) return;

        float distToPlayer = Vector2.Distance(transform.position, target.position);

        // --- 상태 전환 (FSM) ---
        if (currentState == EnemyState.Patrol)
        {
            if (distToPlayer <= detectionRadius) 
                currentState = EnemyState.Chase;
        }
        else if (currentState == EnemyState.Chase)
        {
            // [수정] 사거리 내에 있고, 돌진 쿨타임이 준비되었을 때만 Attack 상태로 진입
            if (distToPlayer <= attackRange && attackTimer >= dashCooldown) 
                currentState = EnemyState.Attack;
            else if (distToPlayer > detectionRadius * 1.5f) 
                currentState = EnemyState.Patrol;
        }

        // --- 상태별 행동 실행 ---
        switch (currentState)
        {
            case EnemyState.Patrol:
                ExecutePatrol();
                break;
            case EnemyState.Chase:
                ExecuteChaseBehavior(distToPlayer);
                break;
            case EnemyState.Attack:
                ExecuteAttack();
                break;
        }
}

private void ExecutePatrol()
{
    if (Vector2.Distance(transform.position, patrolTarget) < 3f)
    {
        patrolTarget = (Vector2)transform.position + new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f));
    }
    MoveTowardsWithPhysics(patrolTarget, speed * 0.5f);
}

// [추가] 쿨타임 중일 때의 추적/배회 로직 분리
private void ExecuteChaseBehavior(float distToPlayer)
{
    // 쿨타임이 도는 중인데 거리가 너무 가깝다면 살짝 물러나거나 평행 이동(배회)
    if (distToPlayer < attackRange * 0.8f && attackTimer < dashCooldown)
    {
        // 플레이어 주위를 도는 움직임 유도 (스피드 감소)
        Vector2 dirToPlayer = (target.position - transform.position).normalized;
        Vector2 orbitVector = new Vector2(-dirToPlayer.y, dirToPlayer.x);
        MoveTowardsWithPhysics((Vector2)transform.position + orbitVector * 3f, speed * 0.6f);
    }
    else
    {
        MoveTowardsWithPhysics(target.position, speed);
    }
}

private void ExecuteAttack()
{
    // 이미 쿨타임 검증을 거치고 들어왔으므로 즉시 코루틴 실행
    StartCoroutine(DashRoutine());
}

private IEnumerator DashRoutine()
{
    isDashing = true;
    attackTimer = 0f;

    // 돌진 전 멈칫 (경고)
    rb.linearVelocity = Vector2.zero;

    if (target != null)
    {
        Vector2 dir = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rb.MoveRotation(angle);
    }

    yield return new WaitForSeconds(waitBeforeDash);

    // [수정] 물리 연산을 억압하지 않고 시작 시 속도만 부여
    Vector2 dashDirection = transform.right;
    rb.linearVelocity = dashDirection * dashSpeed; 

    float timer = 0f;
    while (timer < dashDuration)
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            rb.linearVelocity = Vector2.zero; // 퍼즈 시에만 예외적으로 속도 0 강제
        }
        
        // 매 프레임 속도를 덮어씌우는 코드를 삭제하여, 충돌(박치기) 시 
        // 물리 엔진이 자연스럽게 적을 튕겨내거나 감속시키도록 유도함.
        timer += Time.deltaTime;
        yield return null;
    }

    // 돌진이 끝난 후 쿨타임을 벌기 위해 즉시 상태를 Chase로 돌려보냄
    isDashing = false;
    currentState = EnemyState.Chase; 
}
}