using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(LineRenderer))]
public class ShipController : MonoBehaviour
{
    [Header("Ship Configuration")]
    [SerializeField] private ShipDefinition shipDefinition;
    private ShipRuntimeState runtimeState;
    public IShipStatsProvider StatsProvider => runtimeState;
    public ShipRuntimeState RuntimeState => runtimeState;

    [Header("항해 및 조작 속도 (선박형)")]
    [SerializeField] private float accelLevel = 1.0f;
    [SerializeField] private float ghostAccelLevel = 2.0f;
    [SerializeField] private float forwardAccel = 2.0f;
    [SerializeField] private float backwardAccel = 1.0f;
    [SerializeField] private float autoAccelMult = 1.0f;  

    [Header("항해 및 선박 조작 UI/UX")]
    [SerializeField] private TextMeshProUGUI speedDisplay;

    [Header("운명 궤도 시각 효과와 UI")]
    [Tooltip("결합 범위 표시 오브젝트")]
    [SerializeField] private GameObject attachRangeIndicator;
    [SerializeField] private GameObject fateDeviationIndicator;
    [SerializeField] private GameObject fateLostUI;
    [SerializeField] private Slider fateDeviationSlider;

    [Header("유령선과 슬립스트림 시각 효과")]
    [Tooltip("유령선 오브젝트")]
    [SerializeField] private GameObject ghostShipPrefab;
    [SerializeField] private Material slipstreamLineMaterial;
    [Tooltip("슬립스트림 라인 너비")]
    [SerializeField] private float slipstreamLineWidth = 0.3f;
    private GameObject ghostShipInstance;

    [Header("운명 동기화(Sync) 설정")]
    [Tooltip("결합 범위 내에 들어왔을때 끌어들이는 스프링 강도")]
    [SerializeField] private float syncSpringForce = 1f;

    [Header("슬립스트림(궤적 추적) 설정")]
    [Tooltip("슬립스트림 라인에 작용하는 자력")]
    [SerializeField] private float slipstreamGripForce = 3.0f;

    [Header("항해 경로(Planning) 설정")]
    [SerializeField] private int maxTracePoints = 200;
    [SerializeField] private float rewindSpeed = 100f;
    [SerializeField] private float pathContinueDistance = 3f;
    [SerializeField] private float pointMinDistance = 1f;
    private float rewindTimer = 0f;

    [Header("체력 및 피격 설정")]
    public int CurrentHp => runtimeState != null ? runtimeState.CurrentHp : 0;
    public float maxFateDistance => runtimeState != null ? runtimeState.MaxFateDistance : 15f;
    
    // [추가완료] 피격 파티클 시스템 (배에 미리 부착해두고 Emit만 사용합니다)
    [Tooltip("플레이어가 맞았을 때 튈 파티클 (배의 자식 오브젝트로 미리 넣어두세요)")]
    [SerializeField] private ParticleSystem hitParticleSystem; 

    [Header("실행 페이즈 보간")]
    [SerializeField] private float ghostVisualLerpSpeed = 15f;
    [SerializeField] private float ghostRotationLerpSpeed = 10f;
    [SerializeField] private float syncRotationMultiplier = 2f;
    [SerializeField] private float manualTurnMultiplier = 100f;
    [SerializeField] private float slipstreamLerpSpeed = 5f;
    [SerializeField] private float lostSpeedPenalty = 0.8f;
    [SerializeField] private float lostRecoveryRatio = 0.8f;
    [SerializeField] private int hitParticleMultiplier = 5;

    private float currentSlipstreamMultiplier = 1.0f;

    [Header("사격 컴포넌트")]
    [SerializeField] private CharacterShoot cannonShooter;
    [SerializeField] private float maxCannonDeviation = 40f; 
    [SerializeField] private GameObject cannonSprite;        

    [Header("시각 효과")]
    [SerializeField] private Material routeMaterial;
    [SerializeField] private float lineWidth = 0.3f;

    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private LineRenderer slipstreamLineRenderer;
    
    private List<Vector3> tracePoints = new List<Vector3>();
    private Vector3[] slipstreamBuffer = new Vector3[200];
    private Vector3[] futureBuffer = new Vector3[200];

    private int ghostTargetIndex = 0; 
    private bool isDrawing = false;
    
    private Vector2 ghostShipPos;
    private Vector2 ghostShipVelocity;
    
    public bool IsSynchronized { get; private set; } = true;
    public bool IsLost { get; private set; } = false; 
    public float CurrentFateDeviation { get; private set; }
    
    public float GetTraceProgress() => maxTracePoints > 0 ? 1 - Mathf.Clamp01((float)tracePoints.Count / maxTracePoints) : 0f;

    public bool IsDockedAtPort { get; private set; }

    private bool isQuestRouteActive = false;
    private bool isVoluntarilyDetached = false;

    public event Action<bool> OnSyncStateChanged; 
    public event Action<bool> OnLostStateChanged; 
    
    // [추가완료] 체력 변경 시 UI 등에 알리기 위한 이벤트 <현재 체력, 최대 체력>
    public event Action<int, int> OnHpChanged; 

    private float currentAngle = 0f; 
    public float cannonAngle { get; private set; }

    private void Awake()
    {
        runtimeState = new ShipRuntimeState(shipDefinition);
        runtimeState.OnHpChanged += (cur, max) => OnHpChanged?.Invoke(cur, max);
        runtimeState.OnStatsRecalculated += OnStatsRecalculated;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = runtimeState.GetStat(ShipStatType.WaterFriction);
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (cannonShooter == null) cannonShooter = GetComponentInChildren<CharacterShoot>();

        if (ghostShipPrefab != null)
        {
            ghostShipInstance = Instantiate(ghostShipPrefab, transform.position, Quaternion.identity);
            ghostShipInstance.SetActive(false);

            if (attachRangeIndicator != null)
            {
                float attachThreshold = runtimeState.GetStat(ShipStatType.AttachThreshold);
                attachRangeIndicator.transform.SetParent(ghostShipInstance.transform, false);
                attachRangeIndicator.transform.localPosition = Vector3.zero;
                attachRangeIndicator.transform.localScale = new Vector3(attachThreshold * 2, attachThreshold * 2, 1f);
            }
        }
    }

    private void OnStatsRecalculated()
    {
        rb.linearDamping = runtimeState.GetStat(ShipStatType.WaterFriction);

        if (attachRangeIndicator != null)
        {
            float attachThreshold = runtimeState.GetStat(ShipStatType.AttachThreshold);
            attachRangeIndicator.transform.localScale = new Vector3(attachThreshold * 2, attachThreshold * 2, 1f);
        }
    }

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = routeMaterial;
        lineRenderer.useWorldSpace = true;

        if (ghostShipInstance != null)
        {
            slipstreamLineRenderer = ghostShipInstance.GetComponent<LineRenderer>();
            slipstreamLineRenderer.startWidth = slipstreamLineWidth; 
            slipstreamLineRenderer.endWidth = slipstreamLineWidth;
            slipstreamLineRenderer.material = slipstreamLineMaterial;
            slipstreamLineRenderer.useWorldSpace = true;
        }
        
        if (attachRangeIndicator != null)
        {
            attachRangeIndicator.SetActive(!IsSynchronized);
        }

        // 시작 시 초기 체력 UI 동기화
        OnHpChanged?.Invoke(runtimeState.CurrentHp, runtimeState.MaxHp);
    }

    public void ResetPathForNextWave()
    {
        isDrawing = false;
        isQuestRouteActive = false;
        tracePoints.Clear();
        
        ghostShipPos = transform.position; 
        ghostTargetIndex = 0; 
        
        if (ghostShipInstance != null) ghostShipInstance.transform.position = ghostShipPos;
        
        SetSyncState(true);
        SetLostState(false);
        isVoluntarilyDetached = false;

        UpdateLineRenderer();
    }

    public void ForceDock(Vector2 dockPosition)
    {
        accelLevel = 0f;
        rb.linearVelocity = Vector2.zero;
        transform.position = (Vector3)dockPosition;
        IsDockedAtPort = true;
    }

    public void Undock()
    {
        IsDockedAtPort = false;
    }

    /// <summary>
    /// 퀘스트 시스템이 생성한 경로를 tracePoints에 주입한다.
    /// 기존 유령선 시스템이 그대로 이 경로를 따라 이동한다.
    /// </summary>
    public void SetQuestRoute(List<Vector3> questTracePoints)
    {
        tracePoints.Clear();
        tracePoints.AddRange(questTracePoints);

        ghostShipPos = tracePoints[0];
        ghostTargetIndex = 1;
        isQuestRouteActive = true;

        SetSyncState(true);
        SetLostState(false);
        isVoluntarilyDetached = false;

        if (ghostShipInstance != null)
            ghostShipInstance.transform.position = ghostShipPos;

        UpdateLineRenderer();
    }

    public int GhostTargetIndex => ghostTargetIndex;
    public int TracePointCount => tracePoints.Count;

    private void Update()
    {
        if (IsDockedAtPort) return;
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused) HandlePlanningPhase();
        else if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
        {
            float dt = Time.deltaTime;
            UpdateGhostShipPosition(dt);
            ConsumeTracePoints();
            UpdateGhostShipVisuals();
            UpdateLineRenderer();
            UpdateSyncState();
            UpdateCannonAngle();
            if (InputManager.Instance.Click.WasPressedThisFrame() && cannonShooter != null && !InputManager.Instance.IsPointerOverUI())
                cannonShooter.TryShoot(cannonAngle);
        }
        speedDisplay.text = accelLevel.ToString();
        if (GameManager.Instance.CurrentPhase == GamePhase.RealTime && !IsSynchronized)
        {
            if (InputManager.Instance.Accelerate.WasPressedThisFrame())
            {
                accelLevel = Mathf.Clamp(accelLevel + 1f, -2f, 2f);
            }
            else if (InputManager.Instance.Decelerate.WasPressedThisFrame())
            {
                accelLevel = Mathf.Clamp(accelLevel - 1f, -2f, 2f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsDockedAtPort) { rb.linearVelocity = Vector2.zero; return; }
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused) { rb.linearVelocity = Vector2.zero; return; }
        float fixedDt = Time.fixedDeltaTime;
        if (IsSynchronized) ExecuteSynchronizedMovement(fixedDt);
        else ExecuteManualMovement(fixedDt);  
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    #region Planning Phase
    private void HandlePlanningPhase()
    {
        if (isQuestRouteActive) return; // 퀘스트 경로 활성화 시 수동 그리기 차단
        if (InputManager.Instance.IsPointerOverUI()) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePos);
        mousePos.z = 0f;

        if (InputManager.Instance.RightClick.IsPressed())
        {
            if (tracePoints.Count > 1) 
            {
                rewindTimer += Time.deltaTime * rewindSpeed;
                int pointsToRemove = Mathf.FloorToInt(rewindTimer);
                
                if (pointsToRemove > 0)
                {
                    rewindTimer -= pointsToRemove; 
                    
                    for (int i = 0; i < pointsToRemove; i++)
                    {
                        if (tracePoints.Count > 1) 
                            tracePoints.RemoveAt(tracePoints.Count - 1);
                    }
                    UpdateLineRenderer(); 
                }
            }
            isDrawing = false; 
            return; 
        }
        else { rewindTimer = 0f; }

        if (InputManager.Instance.Click.WasPressedThisFrame())
        {
            if (tracePoints.Count > 1 && Vector3.Distance(mousePos, tracePoints[tracePoints.Count - 1]) < pathContinueDistance)
            {
                isDrawing = true;
            }
            else if (Vector3.Distance(mousePos, transform.position) < pathContinueDistance || tracePoints.Count <= 1)
            {
                isDrawing = true;
                tracePoints.Clear();
                tracePoints.Add(transform.position);
                ghostShipPos = transform.position;
                ghostTargetIndex = 1;

                SetSyncState(true);
                SetLostState(false);
                isVoluntarilyDetached = false;

                if (ghostShipInstance != null) ghostShipInstance.transform.position = ghostShipPos;
                UpdateLineRenderer();
            }
        }
        else if (InputManager.Instance.Click.IsPressed() && isDrawing)
        {
            if (tracePoints.Count > 0 && Vector3.Distance(tracePoints[tracePoints.Count - 1], mousePos) > pointMinDistance)
            {
                if (tracePoints.Count < maxTracePoints)
                {
                    tracePoints.Add(mousePos);
                    UpdateLineRenderer();
                }
            }
        }
        else if (InputManager.Instance.Click.WasReleasedThisFrame()) isDrawing = false;
    }
    #endregion

    #region Execution Phase: Logic & Visuals
    private void UpdateGhostShipPosition(float dt)
    {
        if (tracePoints.Count == 0 || ghostTargetIndex >= tracePoints.Count) 
        {
            ghostShipVelocity = Vector2.zero; return;
        }

        Vector2 target = tracePoints[ghostTargetIndex];
        float dist = Vector2.Distance(ghostShipPos, target);

        if (dist < 0.5f)
        {
            ghostTargetIndex++;
            if (ghostTargetIndex >= tracePoints.Count) { ghostShipVelocity = Vector2.zero; return; } 
            target = tracePoints[ghostTargetIndex];
        }

        Vector2 dir = (target - ghostShipPos).normalized;
        float lostAdventage = IsLost ? lostSpeedPenalty : 1.0f;
        float currentGhostSpeed = ((runtimeState.GetStat(ShipStatType.Speed) + ghostAccelLevel * forwardAccel) * autoAccelMult * lostAdventage);
        
        Vector2 nextPos = ghostShipPos + dir * currentGhostSpeed * dt;
        ghostShipVelocity = (nextPos - ghostShipPos) / dt;
        ghostShipPos = nextPos;
    }

    private void UpdateGhostShipVisuals()
    {
        if (ghostShipInstance == null) return;
        ghostShipInstance.transform.position = Vector3.Lerp(ghostShipInstance.transform.position, ghostShipPos, Time.deltaTime * ghostVisualLerpSpeed);

        if (ghostTargetIndex < tracePoints.Count)
        {
            Vector2 dir = ((Vector2)tracePoints[ghostTargetIndex] - (Vector2)ghostShipInstance.transform.position).normalized;
            if (dir != Vector2.zero)
            {
                float ghostAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Quaternion targetRot = Quaternion.Euler(0, 0, ghostAngle);
                ghostShipInstance.transform.rotation = Quaternion.Slerp(ghostShipInstance.transform.rotation, targetRot, Time.deltaTime * ghostRotationLerpSpeed);
            }
        }
    }

    private void UpdateSyncState()
    {
        // 경로 라인까지의 최단 거리로 동기화 판정 (유령선 기준이 아닌 경로 기반)
        CurrentFateDeviation = GetDistanceToRoutePath();
        bool isHoldingShift = GameManager.Instance.IsSteeringMode;

        float detachThreshold = runtimeState.GetStat(ShipStatType.DetachThreshold);
        float attachThreshold = runtimeState.GetStat(ShipStatType.AttachThreshold);
        float maxFateDist = runtimeState.GetStat(ShipStatType.MaxFateDistance);
        float slipRadius = runtimeState.GetStat(ShipStatType.SlipstreamRadius);

        if (IsSynchronized && isHoldingShift) { SetSyncState(false); isVoluntarilyDetached = true; }
        if (isVoluntarilyDetached && CurrentFateDeviation > slipRadius) isVoluntarilyDetached = false;

        if (!isVoluntarilyDetached)
        {
            if (IsSynchronized && (CurrentFateDeviation > detachThreshold || CurrentFateDeviation >= maxFateDist)) SetSyncState(false);
            else if (!IsSynchronized && CurrentFateDeviation <= attachThreshold) SetSyncState(true);
        }

        if (!IsLost && CurrentFateDeviation >= maxFateDist) SetLostState(true);
        else if (IsLost && CurrentFateDeviation < maxFateDist * lostRecoveryRatio) SetLostState(false);
    }

    /// <summary>
    /// 플레이어에서 경로 라인(tracePoints)까지의 최단 거리를 계산한다.
    /// 경로가 없으면 유령선과의 거리를 폴백으로 사용한다.
    /// </summary>
    private float GetDistanceToRoutePath()
    {
        if (tracePoints.Count < 2)
            return Vector2.Distance(transform.position, ghostShipPos);

        Vector2 playerPos = transform.position;
        float minDistSq = float.MaxValue;

        for (int i = 0; i < tracePoints.Count - 1; i++)
        {
            Vector2 a = tracePoints[i];
            Vector2 b = tracePoints[i + 1];
            Vector2 ab = b - a;
            float sqrLen = ab.sqrMagnitude;

            if (sqrLen < 0.001f) continue;

            float t = Mathf.Clamp01(Vector2.Dot(playerPos - a, ab) / sqrLen);
            Vector2 projection = a + t * ab;
            float distSq = (playerPos - projection).sqrMagnitude;

            if (distSq < minDistSq)
                minDistSq = distSq;
        }

        return Mathf.Sqrt(minDistSq);
    }

    private void SetSyncState(bool state)
    {
        if (IsSynchronized == state) return;
        IsSynchronized = state;
        accelLevel = ghostAccelLevel; 
        if (ghostShipInstance != null) ghostShipInstance.SetActive(!IsSynchronized);
        OnSyncStateChanged?.Invoke(IsSynchronized);
    }

    private void SetLostState(bool state)
    {
        if (IsLost == state) return;
        IsLost = state;
        OnLostStateChanged?.Invoke(IsLost); 
    }

    private bool TryGetSlipstreamData(out Vector2 nearestPoint, out int segmentStartIndex)
    {
        nearestPoint = Vector2.zero; segmentStartIndex = -1;
        if (tracePoints.Count < 2 || ghostTargetIndex < 1) return false;

        Vector2 currentPos = transform.position;
        float minDistSqr = float.MaxValue;
        bool found = false;
        int activeSegments = ghostTargetIndex;

        for (int i = 0; i < tracePoints.Count - 1; i++)
        {
            Vector2 a = tracePoints[i];
            Vector2 b = (i == activeSegments - 1) ? ghostShipPos : (Vector2)tracePoints[i + 1];

            if ((b - a).sqrMagnitude < 0.001f) continue; 

            Vector2 ab = b - a;
            float t = Vector2.Dot(currentPos - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 projection = a + t * ab;

            float distSqr = (currentPos - projection).sqrMagnitude;
            float slipRadius = runtimeState.GetStat(ShipStatType.SlipstreamRadius);
            if (distSqr < slipRadius * slipRadius && distSqr < minDistSqr)
            {
                minDistSqr = distSqr; nearestPoint = projection; segmentStartIndex = i; found = true;
            }
        }
        return found;
    }

    private void ConsumeTracePoints()
    {
        if (tracePoints.Count < 2 || ghostTargetIndex < 2) return;
        Vector2 a = tracePoints[0]; Vector2 b = tracePoints[1]; Vector2 currentPos = transform.position;

        Vector2 ab = b - a; Vector2 ap = currentPos - a;
        float t = Vector2.Dot(ap, ab) / ab.sqrMagnitude;
        Vector2 projection = a + t * ab;
        float consumeSlipRadius = runtimeState.GetStat(ShipStatType.SlipstreamRadius);
        if (t >= 0.8f && (currentPos - projection).sqrMagnitude <= (consumeSlipRadius * consumeSlipRadius))
        {
            tracePoints.RemoveAt(0); ghostTargetIndex--; 
        }
    }
    
    private void EnsureBufferSize(ref Vector3[] buffer, int requiredSize)
    {
        if (buffer == null || buffer.Length < requiredSize)
        {
            int newSize = buffer == null ? 200 : buffer.Length * 2;
            while (newSize < requiredSize) newSize *= 2;
            buffer = new Vector3[newSize];
        }
    }

    private void UpdateLineRenderer()
    {
        if (tracePoints.Count == 0)
        {
            lineRenderer.positionCount = 0;
            if (slipstreamLineRenderer != null) slipstreamLineRenderer.positionCount = 0;
            return;
        }

        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            lineRenderer.positionCount = tracePoints.Count;
            EnsureBufferSize(ref futureBuffer, tracePoints.Count);
            tracePoints.CopyTo(futureBuffer);
            lineRenderer.SetPositions(futureBuffer);
            
            if (slipstreamLineRenderer != null)
            {
                slipstreamLineRenderer.positionCount = tracePoints.Count;
                slipstreamLineRenderer.SetPositions(futureBuffer);
            }
            return;
        }

        if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
        {
            if (ghostTargetIndex > 0 && ghostTargetIndex < tracePoints.Count)
            {
                if (slipstreamLineRenderer != null)
                {
                    int slipCount = ghostTargetIndex + 1;
                    EnsureBufferSize(ref slipstreamBuffer, slipCount);
                    for (int i = 0; i < ghostTargetIndex; i++) slipstreamBuffer[i] = tracePoints[i];
                    slipstreamBuffer[ghostTargetIndex] = ghostShipPos;
                    slipstreamLineRenderer.positionCount = slipCount;
                    slipstreamLineRenderer.SetPositions(slipstreamBuffer);
                }

                int futurePointCount = tracePoints.Count - ghostTargetIndex + 1;
                EnsureBufferSize(ref futureBuffer, futurePointCount);
                futureBuffer[0] = ghostShipPos;
                for (int i = ghostTargetIndex; i < tracePoints.Count; i++) futureBuffer[i - ghostTargetIndex + 1] = tracePoints[i];
                
                lineRenderer.positionCount = futurePointCount;
                lineRenderer.SetPositions(futureBuffer);
            }
            else if (ghostTargetIndex >= tracePoints.Count)
            {
                if (slipstreamLineRenderer != null)
                {
                    slipstreamLineRenderer.positionCount = tracePoints.Count;
                    EnsureBufferSize(ref slipstreamBuffer, tracePoints.Count);
                    tracePoints.CopyTo(slipstreamBuffer);
                    slipstreamLineRenderer.SetPositions(slipstreamBuffer);
                }
                lineRenderer.positionCount = 0;
            }
            else
            {
                lineRenderer.positionCount = tracePoints.Count;
                EnsureBufferSize(ref futureBuffer, tracePoints.Count);
                tracePoints.CopyTo(futureBuffer);
                lineRenderer.SetPositions(futureBuffer);
                if (slipstreamLineRenderer != null) slipstreamLineRenderer.positionCount = 0;
            }
        }
    }
    #endregion

    #region Execution Phase: Physics
    private void ExecuteSynchronizedMovement(float dt)
    {
        Vector2 toGhost = ghostShipPos - (Vector2)transform.position;
        Vector2 catchUpVelocity = toGhost * syncSpringForce;
        rb.linearVelocity = ghostShipVelocity + catchUpVelocity;

        if (ghostShipVelocity.sqrMagnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(ghostShipVelocity.y, ghostShipVelocity.x) * Mathf.Rad2Deg;
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, dt * runtimeState.GetStat(ShipStatType.TurnSpeed) * syncRotationMultiplier);
        }
        currentSlipstreamMultiplier = 1.0f;
    }

    private void ExecuteManualMovement(float dt)
    {
        float turnInput = InputManager.Instance.Steer.ReadValue<float>();
        currentAngle -= turnInput * runtimeState.GetStat(ShipStatType.TurnSpeed) * manualTurnMultiplier * dt;

        float angleRad = currentAngle * Mathf.Deg2Rad;
        Vector2 forwardVec = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        float targetSlipstreamMultiplier = 1.0f;
        Vector2 gripForce = Vector2.zero;

        if (!isVoluntarilyDetached && TryGetSlipstreamData(out Vector2 nearestPoint, out int segmentIndex) && !IsSynchronized)
        {
            targetSlipstreamMultiplier = runtimeState.GetStat(ShipStatType.SlipstreamMultiplier);
            Vector2 dirToRail = (nearestPoint - (Vector2)transform.position).normalized;
            float distToRail = Vector2.Distance(transform.position, nearestPoint);
            gripForce = dirToRail * (distToRail * slipstreamGripForce * (rb.linearVelocity.magnitude * 0.5f));
        }

        currentSlipstreamMultiplier = Mathf.Lerp(currentSlipstreamMultiplier, targetSlipstreamMultiplier, dt * slipstreamLerpSpeed);

        float shipSpeed = runtimeState.GetStat(ShipStatType.Speed);
        float targetSpeed = (accelLevel >= 0) ? shipSpeed + (accelLevel * forwardAccel) : shipSpeed + (accelLevel * backwardAccel);
        if(!IsSynchronized) rb.AddForce((forwardVec * (targetSpeed * currentSlipstreamMultiplier)) + gripForce, ForceMode2D.Force);
        else rb.AddForce((forwardVec * targetSpeed) + gripForce, ForceMode2D.Force);
    }
    #endregion

    private void UpdateCannonAngle()
    {
        if (cannonSprite == null) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePos); mousePos.z = 0f;

        float mouseAngle = Mathf.Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x) * Mathf.Rad2Deg;
        float leftSide = currentAngle + 90f; float rightSide = currentAngle - 90f;
        float diffLeft = Mathf.Abs(Mathf.DeltaAngle(leftSide, mouseAngle)); float diffRight = Mathf.Abs(Mathf.DeltaAngle(rightSide, mouseAngle));
        float baseAngle = (diffLeft < diffRight) ? leftSide : rightSide;
        float delta = Mathf.Clamp(Mathf.DeltaAngle(baseAngle, mouseAngle), -maxCannonDeviation, maxCannonDeviation);
        cannonAngle = baseAngle + delta;
        cannonSprite.transform.rotation = Quaternion.Euler(0f, 0f, cannonAngle);
    }

    public void TakeDamage(int damage)
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused) return;

        runtimeState.TakeDamage(damage);

        // 성능 최적화와 타격감을 위한 Emit 방식 파티클 방출
        if (hitParticleSystem != null)
        {
            hitParticleSystem.Emit(damage * hitParticleMultiplier);
        }

        // 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerDeath();
        }

        if (runtimeState.CurrentHp <= 0)
        {
            GameManager.Instance.GameOver();
        }
    }
}