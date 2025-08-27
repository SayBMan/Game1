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
        StartCoroutine(HandleWaveEnd());
    }

    private IEnumerator HandleWaveEnd()
    {
        yield return new WaitForSecondsRealtime(interWaveDelay);

        GameObject[] choices = PickRandomUniqueByType(allPowerupPrefabs, choicesPerWave);

        if (waveUI != null)
        {
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

 
    private GameObject[] PickRandomUniqueByType(GameObject[] source, int count)
    {
        if (source == null || source.Length == 0 || count <= 0) return new GameObject[0];

        var pool = new List<GameObject>();
        pool.AddRange(source);
        pool.RemoveAll(x => x == null);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = pool[i];
            pool[i] = pool[j];
            pool[j] = tmp;
        }

        var groups = new Dictionary<string, List<GameObject>>();

        foreach (var prefab in pool)
        {
            string key = GetTypeKey(prefab);
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<GameObject>();
                groups[key] = list;
            }
            list.Add(prefab);
        }

        var keys = new List<string>(groups.Keys);

        for (int i = keys.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = keys[i];
            keys[i] = keys[j];
            keys[j] = tmp;
        }

        var chosen = new List<GameObject>();
        int take = Mathf.Min(count, keys.Count);
        for (int i = 0; i < take; i++)
        {
            string k = keys[i];
            var list = groups[k];
            var pick = list[Random.Range(0, list.Count)];
            chosen.Add(pick);
        }

        return chosen.ToArray();
    }

    private string GetTypeKey(GameObject prefab)
    {
        if (prefab == null) return "__null__";
        var coll = prefab.GetComponent<Collectible>();
        if (coll != null && coll.data != null)
        {
            return coll.data.itemType.ToString();
        }

        return "__PREFAB__:" + prefab.name;
    }
}
