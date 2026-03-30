using UnityEngine;

[CreateAssetMenu(fileName = "FoWConfig", menuName = "VTZ/Fog of War Config")]
public class FoWConfig : ScriptableObject
{
    [Header("Grid Settings")]
    [Tooltip("마스크 텍스처 해상도 (월드맵 전체를 커버하는 픽셀 수)")]
    public int textureResolution = 512;

    [Header("Vision")]
    [Tooltip("시야 반경 (셀 단위)")]
    public int visionRadius = 8;

    [Tooltip("시야 갱신 간격 (초). 0이면 셀 변경 시에만 갱신")]
    public float updateInterval = 0.1f;

    [Header("Brush")]
    [Tooltip("브러시 텍스처 (불규칙 가장자리의 흰색 알파 텍스처). " +
             "Read/Write 활성화 필수.")]
    public Texture2D brushTexture;

    [Tooltip("브러시가 셀 하나를 채우는 픽셀 크기")]
    public int brushSize = 6;

    [Tooltip("인접 브러시 간 오버랩 비율 (0~0.5). " +
             "0.15 = 15% 겹침으로 격자 이질감 제거")]
    [Range(0f, 0.5f)]
    public float brushOverlap = 0.15f;
}
