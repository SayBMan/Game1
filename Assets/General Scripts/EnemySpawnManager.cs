using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public GameObject[] Enemies = new GameObject[2];
    private GameObject player;
    public float spawnX;
    public float spawnY;
    public float spawnTimer;
    public float spawnRate;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnTimer = spawnRate;
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if(player == null) return;
        
        if (spawnTimer <= 0)
        {
            spawnX = Random.Range(-6f, 6f);
            spawnY = Random.Range(-3f, 3f);
            int randomIndex = Random.Range(0, Enemies.Length);
            Instantiate(Enemies[randomIndex], new Vector3(spawnX, spawnY, 0), Quaternion.identity);
            spawnTimer = spawnRate;
        }
        else
        {
            spawnTimer -= Time.deltaTime;
        }
    }
}
