using UnityEngine;
using System.Collections;

public struct specialAttackInfo
{
    public Vector3 startPos;
    public Vector3 endPos;
    public Vector3 direction;
}

public class BossEnemy : Enemy
{
    [SerializeField] private float dashSpeed;

    [Header("인트로 설정")]
    [SerializeField] private float introDownDistance = 10f;
    [SerializeField] private float introDuration = 2f;

    [Header("패턴2")]
    [SerializeField] private int bulletDamage = 5;
    private bool shootCross = true; // 첫 발사는 상하좌우
    [SerializeField] private Transform[] muzzles; // 8개
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private GameObject bulletPrefab;

    [Header("패턴3")]
    [SerializeField] private float alertTime = 2f;
    [SerializeField] private Color pathColor = new Color(1, 0, 0, 0.5f);
    private specialAttackInfo[] specialInfo = new specialAttackInfo[4];
    private Camera mainCam;
    private Vector3 direction;
    private float initialWidth;


    private LineRenderer lineRenderer;
    private Vector2 _originalPosition;
    private Vector3 _originalScale;

    protected override void Awake()
    {
        base.Awake();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        mainCam = Camera.main;
    }

    protected override void Update() {}

    private void OnEnable()
    {
        _originalPosition = transform.position;
        _originalScale = transform.localScale;
        StartCoroutine(BossIntroSequence());
    }

    private IEnumerator PausedWait(float time)
    {
        return CoroutineHelper.PausedWait(time);
    }


    #region 보스 인트로 
    private IEnumerator BossIntroSequence()
    {
        AudioManager.Instance.PlayBossAppear();
        // 시작 위치 위로 순간이동
        Vector2 startPosition = _originalPosition + (Vector2.up * introDownDistance);
        transform.position = startPosition;

        // 인트로 시간 동안 부드럽게 하강
        float timer = 0f;
        while (timer < introDuration)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }
            timer += Time.deltaTime;
            float t = timer / introDuration;
            
            t = t * t * (3f - 2f * t);

            transform.position = Vector2.Lerp(startPosition, _originalPosition, t);
            yield return null;
        }

        // 위치 보정
        transform.position = _originalPosition;

        yield return StartCoroutine(ShowWarningEffect(1f));
        yield return StartCoroutine(PausedWait(1f));
        
        StartCoroutine(PatternLoop());
    }

    /// <summary>
    /// 지정된 시간 동안 보스가 진동합니다.
    /// </summary>
    private IEnumerator ShowWarningEffect(float duration)
    {
        float timer = 0f;
        float shakeMagnitude = 0.05f; 

        while (timer < duration)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }

            float xOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
            float yOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
            
            transform.position = _originalPosition + new Vector2(xOffset, yOffset);

            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = _originalPosition;
    }
    #endregion

    // 보스 공격 패턴 루프
    private IEnumerator PatternLoop()
    {
        while (true)
        {
            Dash();
            yield return StartCoroutine(PausedWait(1f));
            Dash();
            yield return StartCoroutine(PausedWait(1f));
            StartCoroutine(BackToOriginalPosition(transform.position));
            yield return StartCoroutine(PausedWait(2f));
            Shoot();
            yield return StartCoroutine(PausedWait(0.5f));
            Shoot();
            yield return StartCoroutine(PausedWait(0.5f));
            Shoot();
            yield return StartCoroutine(PausedWait(2f));
            yield return StartCoroutine(SpecialAttack());
            yield return StartCoroutine(PausedWait(2f));
        }
    }

    #region 패턴 1: 대쉬 공격
    private void Dash()
    {
        AudioManager.Instance.PlayEpicMobDash();
        Vector2 dir = target != null
            ? (target.position - transform.position).normalized
            : transform.right;
        StartCoroutine(DashAttackRoutine(dir));
    }

    IEnumerator DashAttackRoutine(Vector2 dir)
    {
        while (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            yield return null;
            continue;
        }

        spriteRenderer.color = Color.red; 
        yield return StartCoroutine(PausedWait(0.25f));
        
        spriteRenderer.color = Color.green;
        float dashDuration = 0.25f; 
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }
            transform.position += (Vector3)dir * dashSpeed * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator BackToOriginalPosition(Vector2 startPosition)
    {
        transform.localScale = new Vector3(2f, 2f, 2f);
        transform.position = startPosition;

        float returnDuration = 0.5f; 
        float elapsedTime = 0f;

        while (elapsedTime < returnDuration)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }

            transform.position = Vector2.Lerp(startPosition, _originalPosition, elapsedTime / returnDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = _originalPosition;
    }
    #endregion

    #region 패턴 2: 총알 발사
    void Shoot()
    {
        AudioManager.Instance.PlayEpicMobShoot();
        for (int i = 0; i < muzzles.Length; i++)
        {
            // 상하좌우
            if (shootCross && i % 2 == 0)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, muzzles[i].position, muzzles[i].rotation);
                bulletObj.GetComponent<BossBullet>().Initialize(muzzles[i].right, bulletSpeed, bulletDamage, transform);
            }

            if (!shootCross && i % 2 == 1)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, muzzles[i].position, muzzles[i].rotation);
                bulletObj.GetComponent<BossBullet>().Initialize(muzzles[i].right, bulletSpeed, bulletDamage, transform);
            }
        }

        // 다음 Shoot 때 패턴 변경
        shootCross = !shootCross;
    }
    #endregion

    #region 패턴 3: 특수 공격
    private specialAttackInfo ReadySpecialAttack(int step)
    {
        specialAttackInfo info = new specialAttackInfo();

        if (mainCam != null)
        {
            float camHalfHeight = mainCam.orthographicSize;
            float camHalfWidth = camHalfHeight * mainCam.aspect;
            Vector3 camPos = mainCam.transform.position;

            // 화면의 95% 안전 구역 설정
            float safeY = camHalfHeight * 0.95f;
            float safeX = camHalfWidth * 0.95f;

            // 화면 밖 등장/퇴장 기준선 (카메라 크기 + 여유값 2f)
            float outX = camHalfWidth + 2f;
            float outY = camHalfHeight + 2f;

            Vector3 startPos = Vector3.zero;
            
            // 정해진 방향과 위치를 세팅
            switch (step)
            {
                case 0: // 위
                    startPos = new Vector3(Random.Range(-safeX, safeX) + camPos.x, camPos.y + outY, 0f);
                    direction = Vector3.down;
                    break;
                case 1: // 왼쪽
                    startPos = new Vector3(camPos.x - outX, Random.Range(-safeY, safeY) + camPos.y, 0f);
                    direction = Vector3.right;
                    break;
                case 2: // 아래
                    startPos = new Vector3(Random.Range(-safeX, safeX) + camPos.x, camPos.y - outY, 0f);
                    direction = Vector3.up;
                    break;
                case 3: // 오른쪽
                    startPos = new Vector3(camPos.x + outX, Random.Range(-safeY, safeY) + camPos.y, 0f);
                    direction = Vector3.left;
                    break;
            }

            // 도착 지점 계산 (내 위치에서 반대편 화면 밖까지)
            Vector3 endPos = startPos + direction * (Mathf.Abs(direction.x) > 0 ? outX * 2 : outY * 2);

            info.startPos = startPos;
            info.endPos = endPos;
            info.direction = direction;
        }

        return info;
    }

    private IEnumerator SpecialAttack()
    {
        Vector2 startPosition = transform.position;
        Vector2 targetPosition = startPosition + (Vector2.up * introDownDistance);

        Vector3 startScale = _originalScale;
        Vector3 targetScale = startScale * 2f;

        float scaleDuration = 1f;
        float scaleTimer = 0f;

        while (scaleTimer < scaleDuration)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }

            scaleTimer += Time.deltaTime;
            float t = scaleTimer / scaleDuration;

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            transform.position = Vector2.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        transform.position = targetPosition;
        transform.localScale = targetScale;

        yield return StartCoroutine(PausedWait(1f));

        LineRenderer[] warningLines = new LineRenderer[4];

        initialWidth = 1.0f;

        for (int i = 0; i < 4; i++)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }

            specialInfo[i] = ReadySpecialAttack(i);

            GameObject lineObj = new GameObject($"WarningLine_{i}");
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            lr.positionCount = 2;
            lr.SetPosition(0, specialInfo[i].startPos);
            lr.SetPosition(1, specialInfo[i].endPos);
            lr.startWidth = initialWidth;
            lr.endWidth = initialWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = pathColor;
            lr.endColor = pathColor;

            warningLines[i] = lr;
            
            AudioManager.Instance.PlayLaserWarning();

            yield return StartCoroutine(PausedWait(0.25f));
        }
        
        float timer = alertTime;
        
        for (int i = 0; i < 4; i++)
        {
            float attackAlertTimer = 0.3f;
            while (attackAlertTimer > 0f)
            {
                if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
                {
                    yield return null;
                    continue;
                }

                attackAlertTimer -= Time.deltaTime;
                
                float ratio = Mathf.Clamp01(attackAlertTimer / 0.5f);
                float blinkSpeed = Mathf.Lerp(30f, 5f, ratio);
                float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
                
                Color c = pathColor;
                c.a = alpha;

                warningLines[i].startColor = c;
                warningLines[i].endColor = c;

                warningLines[i].startWidth = initialWidth * ratio;
                warningLines[i].endWidth = initialWidth * ratio;

                yield return null;
            }

            Destroy(warningLines[i].gameObject);

            yield return StartCoroutine(SpecialAttackDash(specialInfo[i]));
        }

        yield return StartCoroutine(BackToOriginalPosition(_originalPosition + (Vector2.up * introDownDistance)));
    }

    private IEnumerator SpecialAttackDash(specialAttackInfo info)
    {
        AudioManager.Instance.PlayEpicMobDash();
        transform.position = info.startPos;

        float dashTimer = 0f;
        float dashDuration = 0.5f;

        while (dashTimer < dashDuration)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }

            dashTimer += Time.deltaTime;
            transform.position += info.direction * speed * Time.deltaTime;
            yield return null;
        }
    }
    #endregion
}