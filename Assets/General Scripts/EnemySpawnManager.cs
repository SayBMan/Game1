using System;
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

    [Header("Spawn area")]
    public float spawnRangeX = 18f;
    public float spawnRangeXneg = -8f;
    public float spawnRangeY = 8f;
    public float spawnRangeYneg = -10f;
    public float minDistanceFromPlayer = 2f;

    [Header("Spawn timing")]
    public float defaultSpawnRate = 1.0f; // seconds between spawns (real time)

    private int aliveEnemies = 0;
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

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
    }

    public void StartWave(int enemiesToSpawn, float spawnRate, Action onWaveComplete)
    {
        if (isSpawning) return;
        spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(enemiesToSpawn, spawnRate > 0 ? spawnRate : defaultSpawnRate, onWaveComplete));
    }

    IEnumerator SpawnWaveCoroutine(int enemiesToSpawn, float spawnRate, Action onWaveComplete)
    {
        isSpawning = true;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            bool spawned = false;
            int attempts = 0;
            while (!spawned && attempts < 50)
            {
                attempts++;
                if (FindValidSpawnPosition(out Vector2 spawnPos))
                {
                    if (Enemies != null && Enemies.Length > 0)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, Enemies.Length);
                        GameObject enemyPrefab = Enemies[randomIndex];
                        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                        aliveEnemies++;
                        spawned = true;
                    }
                    else break;
                }
                yield return new WaitForSecondsRealtime(0.02f);
            }

            if (!spawned)
            {
                // fallback random position
                Vector2 fallbackPos = new Vector2(
                    UnityEngine.Random.Range(spawnRangeXneg, spawnRangeX),
                    UnityEngine.Random.Range(spawnRangeYneg, spawnRangeY)
                );
                if (Enemies != null && Enemies.Length > 0)
                {
                    Instantiate(Enemies[UnityEngine.Random.Range(0, Enemies.Length)], fallbackPos, Quaternion.identity);
                    aliveEnemies++;
                }
            }

            yield return new WaitForSecondsRealtime(spawnRate);
        }

        isSpawning = false;

        // wait until all alive enemies are gone
        while (aliveEnemies > 0)
            yield return null;

        onWaveComplete?.Invoke();
    }

    public void NotifyEnemyDied()
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }

    public bool FindValidSpawnPosition(out Vector2 position)
    {
        position = Vector2.zero;
        if (playerTransform == null) return false;

        for (int i = 0; i < 60; i++)
        {
            float spawnX = UnityEngine.Random.Range(spawnRangeXneg, spawnRangeX);
            float spawnY = UnityEngine.Random.Range(spawnRangeYneg, spawnRangeY);
            Vector2 testPos = new Vector2(spawnX, spawnY);

            if (Vector2.Distance(testPos, playerTransform.position) < minDistanceFromPlayer)
                continue;

            if (obstacleTilemap != null)
            {
                Vector3Int cellPos = obstacleTilemap.WorldToCell(testPos);
                if (obstacleTilemap.GetTile(cellPos) != null) continue;
            }

            position = testPos;
            return true;
        }

        return false;
    }
}
