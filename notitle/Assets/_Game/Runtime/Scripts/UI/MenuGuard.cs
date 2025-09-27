// MenuGuard.cs  (StartScreen 전용)
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
using static UnityEngine.EventSystems.StandaloneInputModule;
#endif

[DefaultExecutionOrder(-10000)]
public class MenuGuard : MonoBehaviour
{
    void Awake()
    {
        // 커서 보이기/잠금 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 메뉴에서 PauseManager 영향 차단
        var pm = Object.FindFirstObjectByType<PauseManager>(UnityEngine.FindObjectsInactive.Include);
        if (pm) pm.enabled = false;

        // EventSystem 1개 보장 + 입력 모듈 충돌 방지
        var es = EventSystem.current ?? new GameObject("EventSystem").AddComponent<EventSystem>();
        if (!es.GetComponent<StandaloneInputModule>() /* && !es.GetComponent<InputModule>() */)
        {
            // 빠른 복구: Standalone 우선(새 InputSystem 안정화 전)
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
    }
    void OnDestroy()
    {
        // 게임 씬으로 넘어갈 때 PauseManager 다시 활성화
        var pm = Object.FindFirstObjectByType<PauseManager>(UnityEngine.FindObjectsInactive.Include);
        if (pm) pm.enabled = true;
    }
}

