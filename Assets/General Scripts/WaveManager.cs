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
            // spawn manager'in StartWave(argh...) signature'ına göre uyarlayın
            // Eğer sizin spawnManager farklı parametre bekliyorsa küçükçe düzenleyin
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

        // Powerup seçimlerini hazırla (unique by ItemType)
        GameObject[] choices = PickRandomUniqueByType(allPowerupPrefabs, choicesPerWave);

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

 
    private GameObject[] PickRandomUniqueByType(GameObject[] source, int count)
    {
        if (source == null || source.Length == 0 || count <= 0) return new GameObject[0];

        // build map: typeKey -> list of prefabs with that type
        var pool = new List<GameObject>();
        pool.AddRange(source);
        pool.RemoveAll(x => x == null);

        // shuffle pool (Fisher-Yates) to ensure randomness within groups
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = pool[i];
            pool[i] = pool[j];
            pool[j] = tmp;
        }

        // dictionary keyed by "typeKey" (string) so we can handle missing Collectible gracefully
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

        // now pick up to 'count' distinct keys; for each key select one random prefab from that group's list
        var keys = new List<string>(groups.Keys);

        // shuffle keys to randomize which types are picked first
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
            // list is already in random order (we shuffled pool), but pick one at random to be safe
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
            // itemType varsa onu kullan (tam olarak tekilleştirir)
            return coll.data.itemType.ToString();
        }

        // fallback: prefab adıyla tekilleştir
        return "__PREFAB__:" + prefab.name;
    }
}
