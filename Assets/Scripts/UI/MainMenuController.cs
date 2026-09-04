using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private GameObject settingsPanel;

    private const string GameplaySceneName = "Gameplay";
    private const string HasSaveKey = "HasSave"; // stand-in for a future save system

    private void Start()
    {
        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(OnContinue);
        settingsButton.onClick.AddListener(OnOpenSettings);
        settingsBackButton.onClick.AddListener(OnCloseSettings);
        quitButton.onClick.AddListener(OnQuit);

        continueButton.interactable = PlayerPrefs.GetInt(HasSaveKey, 0) == 1;
        settingsPanel.SetActive(false);
    }

    private void OnNewGame()
    {
        SceneManager.LoadScene(GameplaySceneName);
    }

    private void OnContinue()
    {
        // TODO: load real save data before switching scenes once a save system exists.
        SceneManager.LoadScene(GameplaySceneName);
    }

    private void OnOpenSettings()
    {
        buttonPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void OnCloseSettings()
    {
        settingsPanel.SetActive(false);
        buttonPanel.SetActive(true);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
