using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class ChestController : MonoBehaviour
{
    public GameObject[] collectiblePrefab;
    public Transform collectibleSpawnPoint;
    private Animator animator;
    private GameObject colObj;
    private Vector3 spawnPos;
    public float holdTime = 0.5f;
    public float jumpPower = 1.5f;
    public float jumpDuration = 0.8f;

    private float timer = 0f;
    private bool isOpening = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isOpening) return;

        if (collision.CompareTag("Player"))
        {
            timer += Time.deltaTime;
            if (timer >= holdTime)
            {
                OpenChest();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            timer = 0f;
        }
    }

    private void OpenChest()
    {
        isOpening = true;

        if (animator != null) animator.SetTrigger("Open");

        // Spawn pozisyonu
        spawnPos = collectibleSpawnPoint != null ? collectibleSpawnPoint.position : transform.position + Vector3.up * 0.5f;

        DropCollectible();

        // Hedef: sandığın biraz altı
        Vector3 targetPos = transform.position + Vector3.down * 0.5f;

        if (colObj == null) return;

        // DOTween ile kavisli hareket
        colObj.transform.DOJump(targetPos, jumpPower, 1, jumpDuration)
            .SetEase(Ease.OutQuad);
    }

    public void DropCollectible()
    {
        if (collectiblePrefab == null || collectiblePrefab.Length == 0)
        {
            Debug.LogWarning("ChestController: collectiblePrefab listesi boş! En az bir prefab ekleyin.");
            return;
        }

        GameObject prefabToDrop = PickRandomCollectible();

        if (prefabToDrop == null)
        {
            // Çok nadir: bütün dropChance'ler 0 veya veri hatası. Fallback: rastgele non-null prefab seç.
            Debug.LogWarning("ChestController: Hiç geçerli drop bulunamadı (tüm dropChance = 0 veya veri eksik). Fallback olarak rasgele bir prefab spawnlanacak.");
            // find any non-null prefab
            List<GameObject> nonNull = new List<GameObject>();
            foreach (var p in collectiblePrefab) if (p != null) nonNull.Add(p);
            if (nonNull.Count == 0) return;
            prefabToDrop = nonNull[Random.Range(0, nonNull.Count)];
        }

        colObj = Instantiate(prefabToDrop, spawnPos, Quaternion.identity);
    }

    private GameObject PickRandomCollectible()
    {
        // build list of valid drops (dropChance > 0)
        var valid = new List<(GameObject prefab, float weight)>(collectiblePrefab.Length);
        foreach (GameObject prefab in collectiblePrefab)
        {
            if (prefab == null) continue;
            Collectible collectible = prefab.GetComponent<Collectible>();
            if (collectible == null || collectible.data == null) continue;

            float weight = collectible.data.dropChance;
            if (weight <= 0f) continue; // kesinlikle seçilmesin

            valid.Add((prefab, weight));
        }

        if (valid.Count == 0)
        {
            // Hiç geçerli yok -> caller fallback uygulasın
            return null;
        }

        // toplam ağırlık
        float totalWeight = 0f;
        for (int i = 0; i < valid.Count; i++) totalWeight += valid[i].weight;

        // güvenlik: totalWeight sıfır olamaz çünkü weight > 0 filtrelendi ama kontrol edelim
        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < valid.Count; i++)
        {
            cumulative += valid[i].weight;
            if (roll <= cumulative)
            {
                return valid[i].prefab;
            }
        }

        // teoretik olarak buraya gelmemeli, ama güvenlik için son elemanı dön
        return valid[^1].prefab;
    }
}
