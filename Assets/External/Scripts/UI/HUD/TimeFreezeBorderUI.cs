using UnityEngine;
using UnityEngine.UI;

public class TimeFreezeBorderUI : MonoBehaviour
{
    [Header("Border RectTransforms")]
    [SerializeField] private RectTransform topBorder;
    [SerializeField] private RectTransform bottomBorder;
    [SerializeField] private RectTransform leftBorder;
    [SerializeField] private RectTransform rightBorder;

    [Header("Dim Overlay")]
    [SerializeField] private Image dimImage;
    [SerializeField] [Range(0f, 1f)] private float dimAlphaWhenPressed = 0.6f;

    [Header("Border Shader")]
    [SerializeField] private Color borderColor = new Color(0f, 0.827451f, 0.9490196f, 1f);
    [SerializeField] [Range(0.05f, 1f)] private float gradientSoftness = 0.45f;
    [SerializeField] [Range(0f, 4f)] private float idleGlowStrength = 0.9f;
    [SerializeField] [Range(0f, 4f)] private float activeGlowStrength = 1.7f;
    [SerializeField] [Range(0f, 4f)] private float particleDensity = 1.4f;
    [SerializeField] [Range(0f, 5f)] private float particleSpeed = 1.25f;
    [SerializeField] [Range(0f, 2f)] private float idleParticleIntensity = 0.45f;
    [SerializeField] [Range(0f, 2f)] private float activeParticleIntensity = 0.95f;

    [Header("Settings")]
    [SerializeField] private float targetThickness = 50f;
    [SerializeField] private float transitionSpeed = 5f;

    private const string BorderShaderName = "UI/FreezeBorderParticles";
    private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
    private static readonly int EdgeDirectionId = Shader.PropertyToID("_EdgeDirection");
    private static readonly int GradientSoftnessId = Shader.PropertyToID("_GradientSoftness");
    private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
    private static readonly int ParticleDensityId = Shader.PropertyToID("_ParticleDensity");
    private static readonly int ParticleSpeedId = Shader.PropertyToID("_ParticleSpeed");
    private static readonly int ParticleIntensityId = Shader.PropertyToID("_ParticleIntensity");
    private static readonly int FlowBlendId = Shader.PropertyToID("_FlowBlend");

    private float currentThickness;
    private float currentFlowBlend;
    private float currentDimAlpha;
    private Material topBorderMaterial;
    private Material bottomBorderMaterial;
    private Material leftBorderMaterial;
    private Material rightBorderMaterial;

    private void Awake()
    {
        if (dimImage == null)
        {
            Transform dimTransform = transform.Find("Dim");
            if (dimTransform != null)
            {
                dimImage = dimTransform.GetComponent<Image>();
            }
        }

        SetupBorderMaterials();
        ApplyDim(0f);
    }

    private void OnDestroy()
    {
        DestroyBorderMaterial(topBorderMaterial);
        DestroyBorderMaterial(bottomBorderMaterial);
        DestroyBorderMaterial(leftBorderMaterial);
        DestroyBorderMaterial(rightBorderMaterial);
    }

    private void Update()
    {
        // GameManager 인스턴스가 없으면 예외 방지
        if (GameManager.Instance == null) return;

        // 사용자의 키 입력 대신 GameManager의 상태를 확인하여 UI 연출 방향 결정
        bool isTimeFrozen = (GameManager.Instance.CurrentPhase == GamePhase.Paused);

        float targetThicknessValue = isTimeFrozen ? targetThickness : 0f;
        float targetFlowBlend = isTimeFrozen ? 1f : 0f;
        
        // 단절 상태이며 운명 일치율이 50% 이하로 떨어졌을 때 Dim 서서히 등장
        float targetDimAlpha = 0f;
        if (GameManager.Instance.IsDetached)
        {
            float syncPercentage = GameManager.Instance.GetFateSyncPercentage();
            if (syncPercentage <= 0.5f)
            {
                // 50% -> 0.0 Alpha, 10% -> 최대 Alpha(dimAlphaWhenPressed)
                float t = Mathf.InverseLerp(0.5f, 0.1f, syncPercentage);
                targetDimAlpha = Mathf.Lerp(0f, dimAlphaWhenPressed, t);
            }
        }

        // unscaledDeltaTime을 사용하여 TimeScale = 0(일시정지)일 때도 부드럽게 UI가 애니메이션 되도록 처리
        currentThickness = Mathf.MoveTowards(
            currentThickness,
            targetThicknessValue,
            targetThickness * transitionSpeed * Time.unscaledDeltaTime
        );

        currentFlowBlend = Mathf.MoveTowards(
            currentFlowBlend,
            targetFlowBlend,
            transitionSpeed * Time.unscaledDeltaTime
        );

        currentDimAlpha = Mathf.MoveTowards(
            currentDimAlpha,
            targetDimAlpha,
            transitionSpeed * Time.unscaledDeltaTime
        );

        ApplyBorder(currentThickness);
        ApplyDim(currentDimAlpha);
        UpdateBorderShaderState(currentFlowBlend);
    }

    private void ApplyBorder(float thickness)
    {
        if (topBorder != null)
        {
            topBorder.sizeDelta = new Vector2(0f, thickness);
        }

        if (bottomBorder != null)
        {
            bottomBorder.sizeDelta = new Vector2(0f, thickness);
        }

        if (leftBorder != null)
        {
            leftBorder.sizeDelta = new Vector2(thickness, 0f);
        }

        if (rightBorder != null)
        {
            rightBorder.sizeDelta = new Vector2(thickness, 0f);
        }
    }

    private void ApplyDim(float alpha)
    {
        if (dimImage == null)
        {
            return;
        }

        Color color = dimImage.color;
        color.a = alpha;
        dimImage.color = color;
    }

    private void SetupBorderMaterials()
    {
        topBorderMaterial = ApplyBorderMaterial(topBorder, new Vector4(0f, 1f, 0f, 0f));
        bottomBorderMaterial = ApplyBorderMaterial(bottomBorder, new Vector4(0f, -1f, 0f, 0f));
        leftBorderMaterial = ApplyBorderMaterial(leftBorder, new Vector4(-1f, 0f, 0f, 0f));
        rightBorderMaterial = ApplyBorderMaterial(rightBorder, new Vector4(1f, 0f, 0f, 0f));
    }

    private Material ApplyBorderMaterial(RectTransform borderRect, Vector4 edgeDirection)
    {
        if (borderRect == null)
        {
            return null;
        }

        Image borderImage = borderRect.GetComponent<Image>();
        if (borderImage == null)
        {
            return null;
        }

        Shader shader = Shader.Find(BorderShaderName);
        if (shader == null)
        {
            return null;
        }

        Material runtimeMaterial = new Material(shader)
        {
            name = $"Runtime_{borderRect.name}_FreezeBorder"
        };

        runtimeMaterial.SetColor(EdgeColorId, borderColor);
        runtimeMaterial.SetVector(EdgeDirectionId, edgeDirection);
        runtimeMaterial.SetFloat(GradientSoftnessId, gradientSoftness);
        runtimeMaterial.SetFloat(ParticleDensityId, particleDensity);
        runtimeMaterial.SetFloat(ParticleSpeedId, particleSpeed);

        borderImage.material = runtimeMaterial;
        borderImage.color = Color.white;
        UpdateMaterialAnimation(runtimeMaterial, 0f);
        return runtimeMaterial;
    }

    private void UpdateBorderShaderState(float flowBlend)
    {
        UpdateMaterialAnimation(topBorderMaterial, flowBlend);
        UpdateMaterialAnimation(bottomBorderMaterial, flowBlend);
        UpdateMaterialAnimation(leftBorderMaterial, flowBlend);
        UpdateMaterialAnimation(rightBorderMaterial, flowBlend);
    }

    private void UpdateMaterialAnimation(Material runtimeMaterial, float flowBlend)
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        runtimeMaterial.SetFloat(
            GlowStrengthId,
            Mathf.Lerp(idleGlowStrength, activeGlowStrength, flowBlend)
        );
        runtimeMaterial.SetFloat(
            ParticleIntensityId,
            Mathf.Lerp(idleParticleIntensity, activeParticleIntensity, flowBlend)
        );
        runtimeMaterial.SetFloat(FlowBlendId, flowBlend);
    }

    private void DestroyBorderMaterial(Material runtimeMaterial)
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeMaterial);
        }
        else
        {
            DestroyImmediate(runtimeMaterial);
        }
    }
}