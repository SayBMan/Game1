using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("References")]
    public GameObject[] Enemies;
    public Tilemap obstacleTilemap;
    public Transform playerTransform;
    public WaveUIController waveUI;

    [Header("Spawn area")]
    public float spawnRangeX = 18f;
    public float spawnRangeXneg = -8f;
    public float spawnRangeY = 8f;
    public float spawnRangeYneg = -10f;
    public float minDistanceFromPlayer = 2f;

    [Header("Spawn timing")]
    public float spawnRate = 1.0f; // seconds between spawns (real time)
    public float startDelay = 1.0f;
    public float interWaveDelay = 0.2f; // artık çok kısa (opsiyonel)

    [Header("Wave sizing")]
    public int baseEnemiesPerWave = 3;
    public int enemiesPerWaveIncrement = 1;

    [Header("Powerup choices")]
    public GameObject[] allPowerupPrefabs;
    public int choicesPerWave = 3;

    // runtime
    private int currentWave = 1;
    private int enemiesToSpawnThisWave = 0;
    private int spawnedThisWave = 0;
    private int aliveEnemies = 0;
    private bool isSpawningWave = false;

    public bool enableSceneCountFallback = true;
    public int sceneFallbackCheckEveryFrames = 30;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        Instance = this;
    }

    void Start()
    {
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
        if (isSpawningWave) return;
        currentWave = Mathf.Max(1, waveNumber);
        enemiesToSpawnThisWave = baseEnemiesPerWave + (currentWave - 1) * enemiesPerWaveIncrement;
        spawnedThisWave = 0;
        isSpawningWave = true;

        if (waveUI != null) waveUI.ShowWaveLabel(currentWave);

        StartCoroutine(SpawnWaveCoroutine());
    }

    IEnumerator SpawnWaveCoroutine()
    {
        for (int i = 0; i < enemiesToSpawnThisWave; i++)
        {
            bool spawned = false;
            int attempts = 0;
            while (!spawned && attempts < 50)
            {
                attempts++;
                Vector2 spawnPos;
                if (FindValidSpawnPosition(out spawnPos))
                {
                    if (Enemies != null && Enemies.Length > 0)
                    {
                        int randomIndex = Random.Range(0, Enemies.Length);
                        GameObject enemyPrefab = Enemies[randomIndex];
                        var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                        // increment live counter
                        aliveEnemies++;
                        spawnedThisWave++;
                        spawned = true;
                    }
                    else
                    {

                        break;
                    }
                }
                yield return new WaitForSecondsRealtime(0.02f);
            }

            if (!spawned)
            {
                Vector2 fallbackPos = new Vector2(
                    Random.Range(spawnRangeXneg, spawnRangeX),
                    Random.Range(spawnRangeYneg, spawnRangeY)
                );

                if (Enemies != null && Enemies.Length > 0)
                {
                    var go = Instantiate(Enemies[Random.Range(0, Enemies.Length)], fallbackPos, Quaternion.identity);
                    aliveEnemies++;
                    spawnedThisWave++;
                }
            }

            yield return new WaitForSecondsRealtime(spawnRate);
        }

        isSpawningWave = false;

        // wait until all alive enemies are zero (primary check = aliveEnemies)
        int frames = 0;
        while (aliveEnemies > 0)
        {
            if (enableSceneCountFallback)
            {
                frames++;
                if (frames >= sceneFallbackCheckEveryFrames)
                {
                    frames = 0;
                    int realCount = CountEnemiesInScene();
                    if (realCount == 0)
                    {
                        aliveEnemies = 0;
                        break;
                    }
                    else
                    {
                        if (realCount > aliveEnemies)
                            aliveEnemies = realCount;
                    }
                }
            }
            yield return null;
        }

        // küçük delay (opsiyonel) ardından powerup UI'ı hemen göster
        yield return new WaitForSecondsRealtime(interWaveDelay);
        ShowPowerupAndWait();
    }

    private void ShowPowerupAndWait()
    {
        GameObject[] choices = PickRandomUnique(allPowerupPrefabs, Mathf.Min(choicesPerWave, allPowerupPrefabs.Length));

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

        currentWave++;
        StartCoroutine(StartNextWaveDelayed(0.6f));
    }

    public void NotifyEnemyDied()
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }

    private GameObject[] PickRandomUnique(GameObject[] source, int count)
    {
        List<GameObject> pool = new List<GameObject>();
        if (source != null) pool.AddRange(source);
        pool.RemoveAll(x => x == null);
        List<GameObject> chosen = new List<GameObject>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            chosen.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return chosen.ToArray();
    }

    public bool FindValidSpawnPosition(out Vector2 position)
    {
        if (playerTransform == null)
        {
            position = Vector2.zero;
            return false;
        }

        for (int i = 0; i < 60; i++)
        {
            float spawnX = Random.Range(spawnRangeXneg, spawnRangeX);
            float spawnY = Random.Range(spawnRangeYneg, spawnRangeY);
            Vector2 testPos = new Vector2(spawnX, spawnY);

            if (Vector2.Distance(testPos, playerTransform.position) < minDistanceFromPlayer)
                continue;

            if (obstacleTilemap != null)
            {
                Vector3Int cellPos = obstacleTilemap.WorldToCell(testPos);
                if (obstacleTilemap.GetTile(cellPos) != null)
                    continue;
            }

            position = testPos;
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    private int CountEnemiesInScene()
    {
        var gos = GameObject.FindGameObjectsWithTag("Enemy");
        return gos != null ? gos.Length : 0;
    }
}
