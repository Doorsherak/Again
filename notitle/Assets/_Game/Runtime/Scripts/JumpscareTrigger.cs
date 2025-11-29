using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JumpscareTrigger : MonoBehaviour
{
    [Header("참조 오브젝트")]
    public GameObject monster;                 // 점프스케어 몬스터
    public CanvasGroup fadeCanvasGroup;        // 화면 페이드용 CanvasGroup
    public MonoBehaviour playerController;     // 플레이어 조작 스크립트

    [Header("타이밍 설정")]
    public float delayBeforeFade = 0.7f;       // 몬스터 보이는 시간
    public float fadeDuration = 1.0f;          // 화면이 완전히 까매질 때까지 시간
    public float delayBeforeRestart = 0.5f;    // 완전 암전 후 대기 시간

    [Header("사운드 옵션")]
    public AudioSource audioSource;            // 점프스케어용 오디오 소스
    public AudioClip jumpscareClip;            // 비명/효과음 클립

    private bool _triggered = false;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            // 시작 시 투명
            fadeCanvasGroup.alpha = 0f;
        }
        if (monster != null)
        {
            monster.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        StartCoroutine(JumpscareSequence());
    }

    private IEnumerator JumpscareSequence()
    {
        // 1. 플레이어 조작 막기
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // 2. 몬스터 활성화
        if (monster != null)
        {
            monster.SetActive(true);
        }

        // 3. 점프스케어 사운드 재생
        if (audioSource != null && jumpscareClip != null)
        {
            audioSource.PlayOneShot(jumpscareClip);
        }

        // (여기에서 카메라 흔들기, 애니메이션 재생 등을 추가해도 됨)

        // 4. 잠시 보여주기
        yield return new WaitForSeconds(delayBeforeFade);

        // 5. 화면 페이드아웃 (정전 느낌)
        if (fadeCanvasGroup != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / fadeDuration);
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, normalized);
                yield return null;
            }
        }

        // 6. 완전 암전 유지
        yield return new WaitForSeconds(delayBeforeRestart);

        // 7. 현재 씬 다시 로드 (재시작)
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}

