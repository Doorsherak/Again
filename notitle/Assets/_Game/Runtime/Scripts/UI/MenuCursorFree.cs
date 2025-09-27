/* =========================  MenuCursorFree.cs  =========================
 * 메뉴(타이틀/옵션/크레딧 등) 씬에서만 커서를 강제로 보이게 함.
 * EventSystem은 전혀 건드리지 않음(중복 생성/경고 방지).
 */
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public class MenuCursorFree : MonoBehaviour
{
    [SerializeField] string[] menuSceneNames = { "StartScreen", "Options", "Credits" };

    void OnEnable()
    {
        Apply();
        InvokeRepeating(nameof(Apply), 0.1f, 0.25f); // 다른 스크립트가 잠가도 주기 복구
    }
    void OnDisable() => CancelInvoke(nameof(Apply));

    void Apply()
    {
        var sn = SceneManager.GetActiveScene().name;
        if (!IsMenu(sn)) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    bool IsMenu(string sn)
    {
        if (menuSceneNames == null) return false;
        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            var n = menuSceneNames[i];
            if (!string.IsNullOrEmpty(n) &&
                string.Equals(n, sn, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
