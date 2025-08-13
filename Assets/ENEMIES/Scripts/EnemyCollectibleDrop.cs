using UnityEngine;
using System.Collections.Generic;

public class EnemyCollectibleDrop : MonoBehaviour
{
    public GameObject[] collectiblePrefab;

    public void DropCollectible()
    {
        GameObject prefabToDrop = PickRandomCollectible();

        if (prefabToDrop != null)
        {
            Instantiate(prefabToDrop, transform.position, Quaternion.identity);
        }
    }

    private GameObject PickRandomCollectible()
    {
        // Aday listesi
        List<GameObject> possibleDrops = new List<GameObject>();

        foreach (GameObject prefab in collectiblePrefab)
        {
            Collectible collectible = prefab.GetComponent<Collectible>();
            if (collectible == null || collectible.data == null) continue;

            float roll = Random.Range(0,100); // 0–100 arası
            if (roll <= collectible.data.dropChance)
            {
                possibleDrops.Add(prefab); // Bu item düşmeye aday
            }
        }

        // Hiç aday yoksa null döner → hiçbir şey düşmez
        if (possibleDrops.Count == 0)
            return null;

        // Adaylardan rastgele birini seç
        int randomIndex = Random.Range(0, possibleDrops.Count);
        return possibleDrops[randomIndex];
    }
}
