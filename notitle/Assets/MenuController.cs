using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;   // MainMenuPanel
    public GameObject optionsPanel;    // OptionsPanel (초기 비활성)

    [Header("Focus")]
    public Selectable firstOnMain;     // Game Start 버튼
    public Selectable firstOnOptions;  // Back 또는 첫 옵션 위젯

    [Header("Game Scene")]
    public string gameSceneName = "GameScene"; // 빌드에 등록된 씬 이름

    void Start()
    {
        // 시작 상태(Options 꺼두기)
        if (optionsPanel) optionsPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (firstOnMain) EventSystem.current.SetSelectedGameObject(firstOnMain.gameObject);
    }

    // --- OnClick 바인딩용 ---
    public void StartGame()
    {
        // 필요하면 페이드 코루틴으로 감싸도 됨
        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OpenOptions()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
        if (firstOnOptions) EventSystem.current.SetSelectedGameObject(firstOnOptions.gameObject);
    }

    public void BackToMain()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (firstOnMain) EventSystem.current.SetSelectedGameObject(firstOnMain.gameObject);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
