using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    public EnemySpawnManager spawnManager;
    public WaveUIController waveUI;
    public Transform playerTransform;

    [Header("Wave sizing")]
    public int baseEnemiesPerWave = 3;
    public int enemiesPerWaveIncrement = 1;

    [Header("Powerup choices")]
    public GameObject[] allPowerupPrefabs;
    public int choicesPerWave = 3;

    [Header("Timing")]
    public float startDelay = 1f;
    public float interWaveDelay = 0.6f;

    // runtime
    private int currentWave = 1;

    void Start()
    {
        if (spawnManager == null) spawnManager = EnemySpawnManager.Instance;
        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        StartCoroutine(StartNextWaveDelayed(startDelay));
    }

    IEnumerator StartNextWaveDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        StartWave(currentWave);
    }

    public void StartWave(int waveNumber)
    {
        currentWave = Mathf.Max(1, waveNumber);
        int enemies = baseEnemiesPerWave + (currentWave - 1) * enemiesPerWaveIncrement;

        if (waveUI != null) waveUI.ShowWaveLabel(currentWave);

        if (spawnManager != null)
        {
            spawnManager.StartWave(enemies, spawnManager.defaultSpawnRate, OnWaveComplete);
        }
        else
        {
            // fallback: hemen OnWaveComplete çağır
            StartCoroutine(DelayedOnWaveComplete(0.5f));
        }
    }

    private IEnumerator DelayedOnWaveComplete(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        OnWaveComplete();
    }

    private void OnWaveComplete()
    {
        // kısa gecikme ver
        StartCoroutine(HandleWaveEnd());
    }

    private IEnumerator HandleWaveEnd()
    {
        yield return new WaitForSecondsRealtime(interWaveDelay);

        // Powerup seçimlerini hazırla
        GameObject[] choices = PickRandomUnique(allPowerupPrefabs, Mathf.Min(choicesPerWave, allPowerupPrefabs.Length));

        if (waveUI != null)
        {
            // waveUI, seçim yapılana kadar oyunu durdurur ve callback ile sonucu verir
            waveUI.ShowPowerupChoices(choices, OnPowerupChosen);
        }
        else
        {
            OnPowerupChosen(null);
        }
    }

    private void OnPowerupChosen(GameObject chosenPrefab)
    {
        if (chosenPrefab != null && playerTransform != null)
        {
            Vector3 spawnPos = playerTransform.position + Vector3.up * 0.6f;
            Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
        }

        // yeni wave
        currentWave++;
        StartCoroutine(StartNextWaveDelayed(0.6f));
    }

    private GameObject[] PickRandomUnique(GameObject[] source, int count)
    {
        List<GameObject> pool = new List<GameObject>();
        if (source != null) pool.AddRange(source);
        pool.RemoveAll(x => x == null);
        List<GameObject> chosen = new List<GameObject>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            chosen.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return chosen.ToArray();
    }
}
