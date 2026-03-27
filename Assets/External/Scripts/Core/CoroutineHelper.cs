using System.Collections;
using UnityEngine;

/// <summary>
/// Pause-aware 코루틴 유틸리티.
/// GamePhase.Paused 상태에서 타이머를 자동으로 정지시킵니다.
/// </summary>
public static class CoroutineHelper
{
    /// <summary>
    /// 지정된 시간만큼 대기하되, Paused 페이즈 동안은 타이머가 멈춥니다.
    /// </summary>
    public static IEnumerator PausedWait(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }
            timer += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 지정된 시간 후 게임오브젝트를 파괴하되, Paused 페이즈 동안은 타이머가 멈춥니다.
    /// </summary>
    public static IEnumerator DestroyAfterPausedTime(GameObject target, float lifetime)
    {
        float timer = 0f;
        while (timer < lifetime)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(target);
    }
}
