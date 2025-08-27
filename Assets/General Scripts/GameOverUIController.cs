using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUIController : MonoBehaviour
{
    public static GameOverUIController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Behavior")]
    [SerializeField] private bool pauseOnShow = true;

    bool shown;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (panel == null) panel = GetComponentInChildren<CanvasGroup>(true);

        HideImmediate();

        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
    }

    public void Show(string customTitle = null)
    {
        if (shown) return;
        if (panel == null)
        {
            var child = GetComponentInChildren<Transform>(true);
            if (child != null)
            {
                var cg = GetComponentInChildren<CanvasGroup>(true);
                if (cg == null && transform.childCount > 0)
                {
                    var t = transform.GetChild(0);
                    panel = t.gameObject.AddComponent<CanvasGroup>();
                }
                else panel = cg;
            }
            if (panel == null) return;
        }

        shown = true;
        if (titleText != null) titleText.text = string.IsNullOrEmpty(customTitle) ? "GAME OVER" : customTitle;

        panel.gameObject.SetActive(true);
        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;

        if (pauseOnShow) Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (!shown || panel == null) return;
        shown = false;
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);

        if (pauseOnShow) Time.timeScale = 1f;
    }

    void HideImmediate()
    {
        shown = false;
        if (panel == null) return;
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);
    }

    void Restart()
    {
        if (pauseOnShow) Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Quit()
    {
        if (pauseOnShow) Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
