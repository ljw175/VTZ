using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeStopEffect : MonoBehaviour
{
    [SerializeField] private Volume timeStopVolume;
    [SerializeField] private float transitionSpeed = 5f;

    void Update()
    {
        bool isActive = GameManager.Instance.CurrentPhase == GamePhase.Paused;
        float target = isActive ? 1f : 0f;
        timeStopVolume.weight = Mathf.Lerp(
            timeStopVolume.weight,
            target,
            Time.unscaledDeltaTime * transitionSpeed
        );
    }
}