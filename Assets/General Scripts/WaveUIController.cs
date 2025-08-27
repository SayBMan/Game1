using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveUIController : MonoBehaviour
{
    [Header("Wave label")]
    public TMP_Text waveLabel;
    public float waveLabelDuration = 1.2f;

    [Header("Powerup UI")]
    public GameObject powerupPanel;
    public RectTransform optionsContainer;
    public GameObject buttonPrefab;
    public int optionCount = 3;

    [Header("Layout (spacing/padding)")]
    public float optionSpacing = 12f;
    public Vector4 optionPadding = new Vector4(8, 8, 8, 8);

    private List<GameObject> spawnedButtons = new List<GameObject>();
    private Action<GameObject> onChosenCallback;
    public bool IsOpen { get; private set; } = false;

    void Start()
    {
        if (powerupPanel != null) powerupPanel.SetActive(false);

        if (optionsContainer == null && powerupPanel != null)
        {
            optionsContainer = powerupPanel.GetComponentInChildren<RectTransform>();
        }
    }

    #region Wave label
    public void ShowWaveLabel(int waveIndex)
    {
        if (waveLabel == null) return;
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
    public void ShowPowerupChoices(GameObject[] prefabChoices, Action<GameObject> onChosen)
    {
        if (powerupPanel == null || optionsContainer == null || buttonPrefab == null || prefabChoices == null || prefabChoices.Length == 0)
        {
            onChosen?.Invoke(null);
            return;
        }

        var choices = new List<GameObject>();
        foreach (var p in prefabChoices) if (p != null) choices.Add(p);
        if (choices.Count == 0)
        {
            onChosen?.Invoke(null);
            return;
        }

        powerupPanel.SetActive(true);
        Time.timeScale = 0f;
        IsOpen = true;
        onChosenCallback = onChosen;

        EnsureLayoutOnContainer();
        CleanupSpawnedButtons();

        int count = Mathf.Min(optionCount, choices.Count);

        for (int i = 0; i < count; i++)
        {
            var choicePrefab = choices[i];
            CreateOptionButton(choicePrefab);
        }

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

        var icon = FindImageIn(btnGO.transform);
        var nameText = FindTMPTextByName(btnGO.transform, "NameText");
        var descText = FindTMPTextByName(btnGO.transform, "DescriptionText");

        var coll = choicePrefab.GetComponent<Collectible>();
        if (coll != null && coll.data != null)
        {
            if (nameText != null) nameText.text = coll.data.itemName;
            if (icon != null && coll.data.icon != null) icon.sprite = coll.data.icon;

            string desc = !string.IsNullOrEmpty(coll.data.description)
                          ? coll.data.description
                          : $"Value: {coll.data.value}";

            if (descText != null) descText.text = desc;
        }
        else
        {
            if (nameText != null) nameText.text = choicePrefab.name;
            if (descText != null) descText.text = "";
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnOptionSelected(choicePrefab));

        spawnedButtons.Add(btnGO);
    }

    private void OnOptionSelected(GameObject chosenPrefab)
    {
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
        var existing = optionsContainer.GetComponent<LayoutGroup>();
        if (existing != null)
        {
            var grid = existing as GridLayoutGroup;
            if (grid != null)
            {
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

    private TMP_Text FindTMPTextByName(Transform root, string childName)
    {
        var named = root.Find(childName);
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
