using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Basit ve temiz Wave UI controller.
/// - Wave label gösterir (kısa süre).
/// - Powerup seçim ekranı açar; prefabChoices içerisinden butonlar oluşturur.
/// - optionsContainer üzerinde var olan LayoutGroup'u kullanır; yoksa HorizontalLayoutGroup ekler.
/// - optionSpacing / optionPadding ile buton aralığını kontrol edebilirsin.
/// </summary>
public class WaveUIController : MonoBehaviour
{
    [Header("Wave label")]
    public TMP_Text waveLabel;
    public float waveLabelDuration = 1.2f;

    [Header("Powerup UI")]
    public GameObject powerupPanel;        // root panel (genelde inactive)
    public RectTransform optionsContainer; // container for generated buttons (assign in Inspector)
    public GameObject buttonPrefab;        // prefab with Button component
    public int optionCount = 3;            // kaç seçenek gösterilecek

    [Header("Layout (spacing/padding)")]
    public float optionSpacing = 12f;      // HorizontalLayoutGroup.spacing veya Grid hücreleri arasına etki
    public Vector4 optionPadding = new Vector4(8, 8, 8, 8); // left, top, right, bottom (kullanıldığında RectOffset'e çevrilir)

    // dahili
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private Action<GameObject> onChosenCallback;
    public bool IsOpen { get; private set; } = false;

    void Start()
    {
        if (powerupPanel != null) powerupPanel.SetActive(false);

        // Eğer optionsContainer atanmamışsa, powerupPanel içinden otomatik almayı dene
        if (optionsContainer == null && powerupPanel != null)
        {
            optionsContainer = powerupPanel.GetComponentInChildren<RectTransform>();
        }
    }

    #region Wave label
    public void ShowWaveLabel(int waveIndex)
    {
        if (waveLabel == null) return;
        // Kullanıcı Wave numarasını 1 tabanlı görmek isterse burada ayarlanır.
        waveLabel.text = $"Wave {Math.Max(1, waveIndex)}";
        waveLabel.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideWaveLabelAfterDelay());
    }

    private IEnumerator HideWaveLabelAfterDelay()
    {
        yield return new WaitForSecondsRealtime(waveLabelDuration);
        if (waveLabel != null) waveLabel.gameObject.SetActive(false);
    }
    #endregion

    #region Powerup UI
    /// <summary>
    /// prefabChoices: düşüreceğin item prefablari (Collectible içerebilir).
    /// onChosen: seçileni geri çağırır (GameObject prefab).
    /// </summary>
    public void ShowPowerupChoices(GameObject[] prefabChoices, Action<GameObject> onChosen)
    {
        // Basit validasyon
        if (powerupPanel == null || optionsContainer == null || buttonPrefab == null || prefabChoices == null || prefabChoices.Length == 0)
        {
            // güvenle callback çağır
            onChosen?.Invoke(null);
            return;
        }

        // Filtrele: null olanları çıkar
        var choices = new List<GameObject>();
        foreach (var p in prefabChoices) if (p != null) choices.Add(p);
        if (choices.Count == 0)
        {
            onChosen?.Invoke(null);
            return;
        }

        // Aç / pause
        powerupPanel.SetActive(true);
        Time.timeScale = 0f;
        IsOpen = true;
        onChosenCallback = onChosen;

        // Layout ayarla (varsa kullan, yoksa HorizontalLayoutGroup ekle)
        EnsureLayoutOnContainer();

        // Eski butonları temizle
        CleanupSpawnedButtons();

        // Kaç buton oluşturulacak?
        int count = Mathf.Min(optionCount, choices.Count);

        for (int i = 0; i < count; i++)
        {
            var choicePrefab = choices[i];
            CreateOptionButton(choicePrefab);
        }

        // Layout güncelle
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContainer);
    }

    private void CreateOptionButton(GameObject choicePrefab)
    {
        var btnGO = Instantiate(buttonPrefab, optionsContainer, false);
        btnGO.transform.localScale = Vector3.one;

        var btn = btnGO.GetComponent<Button>();
        if (btn == null)
        {
            Destroy(btnGO);
            return;
        }

        // Icon ve isim doldurma (esnek)
        var icon = FindImageIn(btnGO.transform);
        var tmp = FindTMPTextIn(btnGO.transform);

        var coll = choicePrefab.GetComponent<Collectible>();
        if (coll != null && coll.data != null)
        {
            if (tmp != null) tmp.text = coll.data.itemName;
            if (icon != null && coll.data.icon != null) icon.sprite = coll.data.icon;
        }
        else
        {
            if (tmp != null) tmp.text = choicePrefab.name;
        }

        // Buton tıklaması
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnOptionSelected(choicePrefab));

        spawnedButtons.Add(btnGO);
    }

    private void OnOptionSelected(GameObject chosenPrefab)
    {
        // Kapat, resume ve callback
        ClosePowerupUI();
        onChosenCallback?.Invoke(chosenPrefab);
        onChosenCallback = null;
    }

    public void ClosePowerupUI()
    {
        CleanupSpawnedButtons();
        if (powerupPanel != null) powerupPanel.SetActive(false);
        Time.timeScale = 1f;
        IsOpen = false;
    }

    private void CleanupSpawnedButtons()
    {
        foreach (var b in spawnedButtons) if (b != null) Destroy(b);
        spawnedButtons.Clear();
    }
    #endregion

    #region Layout helpers
    private void EnsureLayoutOnContainer()
    {
        // Eğer zaten bir LayoutGroup varsa onu kullan (GridLayoutGroup / Horizontal / Vertical)
        var existing = optionsContainer.GetComponent<LayoutGroup>();
        if (existing != null)
        {
            // Eğer GridLayoutGroup varsa cell spacing veya cell size'ı Inspector'dan ayarla istersin.
            var grid = existing as GridLayoutGroup;
            if (grid != null)
            {
                // grid spacing ayarla
                grid.spacing = new Vector2(optionSpacing, optionSpacing);
                grid.padding = new RectOffset(
                    (int)optionPadding.x, (int)optionPadding.z,
                    (int)optionPadding.y, (int)optionPadding.w);
                return;
            }

            var horiz = existing as HorizontalLayoutGroup;
            if (horiz != null)
            {
                horiz.spacing = optionSpacing;
                horiz.childAlignment = TextAnchor.MiddleCenter;
                horiz.padding = new RectOffset(
                    (int)optionPadding.x, (int)optionPadding.z,
                    (int)optionPadding.y, (int)optionPadding.w);
                return;
            }

            var vert = existing as VerticalLayoutGroup;
            if (vert != null)
            {
                vert.spacing = optionSpacing;
                vert.childAlignment = TextAnchor.MiddleCenter;
                vert.padding = new RectOffset(
                    (int)optionPadding.x, (int)optionPadding.z,
                    (int)optionPadding.y, (int)optionPadding.w);
                return;
            }
        }

        // Hiç layout yoksa HorizontalLayoutGroup ekle ve ayarla
        var h = optionsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = optionSpacing;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        h.padding = new RectOffset(
            (int)optionPadding.x, (int)optionPadding.z,
            (int)optionPadding.y, (int)optionPadding.w);

        // ve ContentSizeFitter ekle (tercihe göre)
        var fitter = optionsContainer.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = optionsContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
    #endregion

    #region Utility: find components in button prefab
    private Image FindImageIn(Transform root)
    {
        var named = root.Find("Icon");
        if (named != null)
        {
            var im = named.GetComponent<Image>();
            if (im != null) return im;
        }

        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject == root.gameObject) continue;
            return img;
        }
        return null;
    }

    private TMP_Text FindTMPTextIn(Transform root)
    {
        var named = root.Find("NameText");
        if (named != null)
        {
            var t = named.GetComponent<TMP_Text>();
            if (t != null) return t;
        }

        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            if (t.gameObject == root.gameObject) continue;
            return t;
        }
        return null;
    }
    #endregion
}
