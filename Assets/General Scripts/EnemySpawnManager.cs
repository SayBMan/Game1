using UnityEngine;
using UnityEngine.Tilemaps; // Önemli

public class EnemySpawnManager : MonoBehaviour
{
    public GameObject[] Enemies;
    public Tilemap obstacleTilemap; // Engellerin olduğu tilemap
    public float spawnRate = 2f;
    public float spawnRangeX = 18f;
    public float spawnRangeXneg = -8f;
    public float spawnRangeY = 8f;
    public float spawnRangeYneg = -10f;
    public float minDistanceFromPlayer = 2f;

    private float spawnTimer;
    private GameObject player;

    void Start()
    {
        spawnTimer = spawnRate;
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (player == null) return;

        if (spawnTimer <= 0)
        {
            Vector2 spawnPos;
            if (FindValidSpawnPosition(out spawnPos))
            {
                int randomIndex = Random.Range(0, Enemies.Length);
                Instantiate(Enemies[randomIndex], spawnPos, Quaternion.identity);
            }
            spawnTimer = spawnRate;
        }
        else
        {
            spawnTimer -= Time.deltaTime;
        }
    }

    bool FindValidSpawnPosition(out Vector2 position)
    {
        for (int i = 0; i < 30; i++) // 30 deneme
        {
            float spawnX = Random.Range(spawnRangeXneg, spawnRangeX);
            float spawnY = Random.Range(spawnRangeYneg, spawnRangeY);
            Vector2 testPos = new Vector2(spawnX, spawnY);

            // Oyuncuya çok yakın olmasın
            if (Vector2.Distance(testPos, player.transform.position) < minDistanceFromPlayer)
                continue;

            // Dünya pozisyonunu tile hücresine çevir
            Vector3Int cellPos = obstacleTilemap.WorldToCell(testPos);

            // Bu hücrede engel tile'ı var mı?
            if (obstacleTilemap.GetTile(cellPos) != null)
                continue; // Engel var → spawn yapma

            // Engel yok → geçerli pozisyon
            position = testPos;
            return true;
        }

        position = Vector2.zero;
        Debug.Log("Enemy spawn failed");
        return false; // Uygun pozisyon bulunamadı
    }
}
