using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUIController : MonoBehaviour
{
    public static GameOverUIController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup panel;          // CanvasGroup on the GameOver panel (recommended)
    [SerializeField] private TextMeshProUGUI titleText;  // optional
    [SerializeField] private Button restartButton;       // optional
    [SerializeField] private Button quitButton;          // optional

    [Header("Behavior")]
    [SerializeField] private bool pauseOnShow = true;

    bool shown;

    void Awake()
    {
        // singleton
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        // try to find panel even if it's child and inactive
        if (panel == null) panel = GetComponentInChildren<CanvasGroup>(true);

        // ensure panel starts hidden
        HideImmediate();

        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
    }

    public void Show(string customTitle = null)
    {
        if (shown) return;
        if (panel == null)
        {
            // Try to find/create a CanvasGroup fallback on the first child named "Panel"
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
