using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// Fog of War 매니저 (오버레이 방식).
/// NativeArray를 활용한 Zero-Allocation 고속 메모리 제어 적용.
/// </summary>
public class FoWManager : MonoBehaviour
{
    public static FoWManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private FoWConfig config;

    [Header("Overlay References")]
    [Tooltip("Sketch Map 오버레이의 SpriteRenderer (VTZ/FoW_SketchOverlay 머티리얼)")]
    [SerializeField] private SpriteRenderer sketchOverlayRenderer;

    [Tooltip("Black 오버레이의 SpriteRenderer (VTZ/FoW_BlackOverlay 머티리얼)")]
    [SerializeField] private SpriteRenderer blackOverlayRenderer;

    [Header("World")]
    [Tooltip("맵 경계를 정의하는 BoxCollider2D (CameraController와 동일한 것 사용)")]
    [SerializeField] private BoxCollider2D mapBounds;

    [Tooltip("추적할 플레이어 Transform (비우면 Player 태그로 자동 탐색)")]
    [SerializeField] private Transform playerTarget;

    // 마스크 텍스처
    private Texture2D exploredMask;
    private Texture2D visionMask;

    // 마스크 픽셀 배열 (NativeArray - 텍스처 메모리 직접 참조)
    private NativeArray<byte> exploredData;
    private NativeArray<byte> visionData;
    
    // 시야 고속 클리어를 위한 0(Black)으로 채워진 더미 배열
    private NativeArray<byte> clearVisionData;

    // 브러시 픽셀 캐시
    private NativeArray<byte> brushData;
    private int brushWidth;
    private int brushHeight;

    // 월드 바운드
    private Vector2 worldMin;
    private Vector2 worldMax;
    private Vector2 worldSize;

    // 셀 추적
    private int texResolution;
    private Vector2Int lastPlayerCell;
    private HashSet<Vector2Int> currentVisionCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> previousVisionCells = new HashSet<Vector2Int>();

    // 갱신 타이머
    private float updateTimer;

    // 머티리얼 인스턴스
    private Material sketchMat;
    private Material blackMat;

    // 셰이더 프로퍼티 ID
    private static readonly int ExploredMaskID = Shader.PropertyToID("_ExploredMask");
    private static readonly int VisionMaskID = Shader.PropertyToID("_VisionMask");
    private static readonly int WorldMinID = Shader.PropertyToID("_WorldMin");
    private static readonly int WorldMaxID = Shader.PropertyToID("_WorldMax");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (playerTarget == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }
    }

    private void Start()
    {
        InitializeBounds();
        InitializeMaskTextures();
        CacheBrush();
        BindMaterials();

        lastPlayerCell = WorldToCell(playerTarget.position);
        RefreshVision(lastPlayerCell, forceFullRebuild: true);
    }

    private void Update()
    {
        if (playerTarget == null || config == null) return;

        Vector2Int currentCell = WorldToCell(playerTarget.position);

        // 셀이 변경되지 않았으면 스킵
        if (currentCell == lastPlayerCell) return;

        // updateInterval 제한
        if (config.updateInterval > 0)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer < config.updateInterval) return;
            updateTimer = 0f;
        }

        lastPlayerCell = currentCell;
        RefreshVision(currentCell, forceFullRebuild: false);
    }

    private void InitializeBounds()
    {
        if (mapBounds != null)
        {
            worldMin = (Vector2)mapBounds.bounds.min;
            worldMax = (Vector2)mapBounds.bounds.max;
        }
        else
        {
            Debug.LogError("[FoW] mapBounds가 할당되지 않았습니다.");
            worldMin = new Vector2(-50, -50);
            worldMax = new Vector2(50, 50);
        }
        worldSize = worldMax - worldMin;
    }

    private void InitializeMaskTextures()
    {
        texResolution = config != null ? config.textureResolution : 512;

        exploredMask = new Texture2D(texResolution, texResolution, TextureFormat.R8, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        visionMask = new Texture2D(texResolution, texResolution, TextureFormat.R8, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        // Texture2D의 내부 메모리 버퍼를 직접 가져옴 (복사본이 아님)
        exploredData = exploredMask.GetRawTextureData<byte>();
        visionData = visionMask.GetRawTextureData<byte>();

        // 고속 클리어를 위한 0 배열 생성 (Persistent 할당)
        clearVisionData = new NativeArray<byte>(exploredData.Length, Allocator.Persistent);

        // 처음에는 둘 다 완전한 어둠(0)으로 초기화
        exploredData.CopyFrom(clearVisionData);
        visionData.CopyFrom(clearVisionData);

        exploredMask.Apply(false);
        visionMask.Apply(false);
    }

    private void CacheBrush()
    {
        int size = config != null ? config.brushSize : 6;
        brushWidth = size;
        brushHeight = size;
        
        // 브러시 데이터도 NativeArray로 캐싱
        brushData = new NativeArray<byte>(size * size, Allocator.Persistent);

        if (config == null || config.brushTexture == null)
        {
            // 브러시 텍스처 미설정 시 원형 폴백
            float center = size * 0.5f;
            float radiusSq = center * center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    brushData[y * size + x] = (dx * dx + dy * dy <= radiusSq) ? (byte)255 : (byte)0;
                }
            }
            return;
        }

        // 브러시 텍스처에서 픽셀 추출
        var srcPixels = config.brushTexture.GetPixels32();
        int srcW = config.brushTexture.width;
        int srcH = config.brushTexture.height;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int srcX = Mathf.Clamp(x * srcW / size, 0, srcW - 1);
                int srcY = Mathf.Clamp(y * srcH / size, 0, srcH - 1);
                var c = srcPixels[srcY * srcW + srcX];
                brushData[y * size + x] = (byte)Mathf.Max(c.a, (c.r + c.g + c.b) / 3);
            }
        }
    }

    private void BindMaterials()
    {
        Vector4 wMin = new Vector4(worldMin.x, worldMin.y, 0, 0);
        Vector4 wMax = new Vector4(worldMax.x, worldMax.y, 0, 0);

        if (sketchOverlayRenderer != null)
        {
            sketchMat = sketchOverlayRenderer.material;
            sketchMat.SetTexture(ExploredMaskID, exploredMask);
            sketchMat.SetTexture(VisionMaskID, visionMask);
            sketchMat.SetVector(WorldMinID, wMin);
            sketchMat.SetVector(WorldMaxID, wMax);
        }

        if (blackOverlayRenderer != null)
        {
            blackMat = blackOverlayRenderer.material;
            blackMat.SetTexture(ExploredMaskID, exploredMask);
            blackMat.SetVector(WorldMinID, wMin);
            blackMat.SetVector(WorldMaxID, wMax);
        }
    }

    /// <summary>
    /// 시야를 갱신한다. NativeArray 직접 수정 및 초고속 클리어 적용.
    /// </summary>
    private void RefreshVision(Vector2Int centerCell, bool forceFullRebuild)
    {
        int radius = config != null ? config.visionRadius : 8;

        var temp = currentVisionCells;
        currentVisionCells = previousVisionCells;
        previousVisionCells = temp;
        currentVisionCells.Clear();

        ComputeCircleCells(centerCell, radius, currentVisionCells);

        // === Vision 마스크: 블록 카피를 통한 0(Black) 초고속 초기화 ===
        visionData.CopyFrom(clearVisionData);

        // === Explored 및 Vision 스탬핑 ===
        foreach (var cell in currentVisionCells)
        {
            // 시야는 항상 현재 보이니까 찍음
            StampBrush(visionData, cell);

            // 탐험 구역은 새로 시야에 들어온 곳이거나 강제 갱신일 때만 찍어서 영구 보존
            if (!previousVisionCells.Contains(cell) || forceFullRebuild)
            {
                StampBrush(exploredData, cell);
            }
        }

        // 데이터가 이미 텍스처 내부 버퍼에 적용되었으므로 SetPixelData 없이 Apply만 호출
        exploredMask.Apply(false);
        visionMask.Apply(false);
    }

    private void ComputeCircleCells(Vector2Int center, int radius, HashSet<Vector2Int> result)
    {
        int rSq = radius * radius;
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy <= rSq)
                {
                    int cx = center.x + dx;
                    int cy = center.y + dy;
                    if (cx >= 0 && cx < texResolution && cy >= 0 && cy < texResolution)
                    {
                        result.Add(new Vector2Int(cx, cy));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 대상 NativeArray 픽셀 배열에 브러시를 스탬핑한다.
    /// </summary>
    private void StampBrush(NativeArray<byte> targetData, Vector2Int cell)
    {
        int startX = cell.x - brushWidth / 2;
        int startY = cell.y - brushHeight / 2;

        for (int by = 0; by < brushHeight; by++)
        {
            int py = startY + by;
            if (py < 0 || py >= texResolution) continue;

            for (int bx = 0; bx < brushWidth; bx++)
            {
                int px = startX + bx;
                if (px < 0 || px >= texResolution) continue;

                int pixelIdx = py * texResolution + px;
                byte brushVal = brushData[by * brushWidth + bx];

                // Max 블렌드 (기존 값보다 밝을 때만 덮어씀)
                if (brushVal > targetData[pixelIdx])
                    targetData[pixelIdx] = brushVal;
            }
        }
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        float nx = (worldPos.x - worldMin.x) / worldSize.x;
        float ny = (worldPos.y - worldMin.y) / worldSize.y;

        int cx = Mathf.Clamp(Mathf.FloorToInt(nx * texResolution), 0, texResolution - 1);
        int cy = Mathf.Clamp(Mathf.FloorToInt(ny * texResolution), 0, texResolution - 1);

        return new Vector2Int(cx, cy);
    }

    public void RevealArea(Vector3 worldCenter, int radiusCells)
    {
        Vector2Int cell = WorldToCell(worldCenter);
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        ComputeCircleCells(cell, radiusCells, cells);

        foreach (var c in cells)
            StampBrush(exploredData, c);

        exploredMask.Apply(false);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 Unmanaged 메모리 해제
        if (clearVisionData.IsCreated) clearVisionData.Dispose();
        if (brushData.IsCreated) brushData.Dispose();

        if (exploredMask != null) Destroy(exploredMask);
        if (visionMask != null) Destroy(visionMask);
        if (sketchMat != null) Destroy(sketchMat);
        if (blackMat != null) Destroy(blackMat);

        if (Instance == this) Instance = null;
    }
}