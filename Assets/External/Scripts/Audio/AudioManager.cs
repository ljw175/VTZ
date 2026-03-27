using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip titleBgm;
    [SerializeField] private AudioClip ingameBgm;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip startButtonClickSfx;
    [SerializeField] private AudioClip laserPlaceSfx;
    [SerializeField] private AudioClip laserWarningSfx;
    [SerializeField] private AudioClip laserMobDashSfx;
    [SerializeField] private AudioClip playerDeathSfx;
    [SerializeField] private AudioClip enemyDeathSfx;
    [SerializeField] private AudioClip enemyDeath2Sfx;
    [SerializeField] private AudioClip bossAppearSfx;
    [SerializeField] private AudioClip bossDeathSfx;
    [SerializeField] private AudioClip epicMobShootSfx;
    [SerializeField] private AudioClip epicMobDashSfx;

    private Coroutine pitchCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PlayTitleBgm();
    }

    #region BGM

    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.pitch = 1f; // 기본 pitch
        bgmSource.Play();
    }

    public void PlayTitleBgm() => PlayBgm(titleBgm);
    public void PlayIngameBgm() => PlayBgm(ingameBgm);

    // 시간 멈출 때
    public void SetSlowBgm()
    {
        ChangePitch(0.3f);
    }

    // 시간 다시 흐를 때
    public void SetNormalBgm()
    {
        ChangePitch(1f);
    }

    private void ChangePitch(float target)
    {
        if (pitchCoroutine != null)
            StopCoroutine(pitchCoroutine);

        pitchCoroutine = StartCoroutine(ChangePitchRoutine(target));
    }

    private IEnumerator ChangePitchRoutine(float target)
    {
        float start = bgmSource.pitch;
        float time = 0f;
        float duration = 0.25f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            bgmSource.pitch = Mathf.Lerp(start, target, time / duration);
            yield return null;
        }

        bgmSource.pitch = target;
    }

    #endregion

    #region SFX

    public void PlaySfx(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonClick() => PlaySfx(buttonClickSfx);
    public void PlayStartButtonClick() => PlaySfx(startButtonClickSfx);
    public void PlayLaserPlace() => PlaySfx(laserPlaceSfx);
    public void PlayLaserWarning() => PlaySfx(laserWarningSfx);
    public void PlayPlayerDeath() => PlaySfx(playerDeathSfx);
    public void PlayEnemyDeath() => PlaySfx(enemyDeathSfx);
    public void PlayBossAppear() => PlaySfx(bossAppearSfx);
    public void PlayEnemyDeath2() => PlaySfx(enemyDeath2Sfx);
    public void PlayBossDeath() => PlaySfx(bossDeathSfx);
    public void PlayEpicMobShoot() => PlaySfx(epicMobShootSfx);
    public void PlayEpicMobDash() => PlaySfx(epicMobDashSfx);
    public void PlayLaserMobDash() => PlaySfx(laserMobDashSfx);

    #endregion
}