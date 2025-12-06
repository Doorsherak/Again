using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;   // MainMenuPanel
    public GameObject optionsPanel;    // OptionsPanel (�ʱ� ��Ȱ��)

    [Header("Focus")]
    public Selectable firstOnMain;     // Game Start ��ư
    public Selectable firstOnOptions;  // Back �Ǵ� ù �ɼ� ����

    [Header("Game Scene")]
    public string gameSceneName = "GameScene"; // ���忡 ��ϵ� �� �̸�

    void Start()
    {
        // ���� ����(Options ���α�)
        if (optionsPanel) optionsPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (firstOnMain) EventSystem.current.SetSelectedGameObject(firstOnMain.gameObject);
    }

    // --- OnClick ���ε��� ---
    public void StartGame()
    {
        // �ʿ��ϸ� ���̵� �ڷ�ƾ���� ���ε� ��
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
