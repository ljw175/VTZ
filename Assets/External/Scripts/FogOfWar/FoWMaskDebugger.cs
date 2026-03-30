using UnityEngine;

/// <summary>
/// 에디터에서 FoW 마스크 텍스처를 시각화하는 디버그 컴포넌트.
/// FoWManager가 있는 오브젝트에 추가하면 Game 뷰 좌하단에 마스크 미리보기를 표시한다.
/// </summary>
public class FoWMaskDebugger : MonoBehaviour
{
    [SerializeField] private bool showExplored = true;
    [SerializeField] private bool showVision = true;
    [SerializeField] private int previewSize = 200;

    private void OnGUI()
    {
        if (FoWManager.Instance == null) return;

        // 리플렉션 없이 접근하기 위해 FoWManager에 디버그 접근자를 사용
        // 실제 빌드에서는 이 컴포넌트를 제거하거나 비활성화
        var fields = typeof(FoWManager).GetField("exploredMask",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var vFields = typeof(FoWManager).GetField("visionMask",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fields == null || vFields == null) return;

        int y = 10;

        if (showExplored)
        {
            var tex = fields.GetValue(FoWManager.Instance) as Texture2D;
            if (tex != null)
            {
                GUI.Label(new Rect(10, y, previewSize, 20), "Explored Mask");
                y += 20;
                GUI.DrawTexture(new Rect(10, y, previewSize, previewSize), tex);
                y += previewSize + 10;
            }
        }

        if (showVision)
        {
            var tex = vFields.GetValue(FoWManager.Instance) as Texture2D;
            if (tex != null)
            {
                GUI.Label(new Rect(10, y, previewSize, 20), "Vision Mask");
                y += 20;
                GUI.DrawTexture(new Rect(10, y, previewSize, previewSize), tex);
            }
        }
    }
}
