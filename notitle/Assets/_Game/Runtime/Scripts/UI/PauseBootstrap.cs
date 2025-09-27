// PauseBootstrap_Safe.cs  (반사/asmdef 의존 제거, 전역 1회 설치)
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DefaultExecutionOrder(-10000)]
public class PauseBootstrap_Safe : MonoBehaviour
{
    static PauseManager instance;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }

        // 1) 기존 매니저가 있나 확인
        instance = Object.FindFirstObjectByType<PauseManager>(UnityEngine.FindObjectsInactive.Include);
        if (instance == null)
        {
            var go = new GameObject("PauseManager_Auto");
            instance = go.AddComponent<PauseManager>();

            // 핵심: 부트스트랩에서 직접 영속화 → v3의 makePersistent 설정 불필요
            DontDestroyOnLoad(go);
        }

        // 2) EventSystem 보장
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem").AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            es.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        // 3) (선택) 에디터에서 바로 확인하고 싶으면 아래 주석 해제
        // instance.Pause();  // 강제로 한 번 띄워 보기
    }
}
