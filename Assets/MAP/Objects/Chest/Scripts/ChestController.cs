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

        animator.SetTrigger("Open");

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

        GameObject prefabToDrop = null;

        // Liste boş olmayana kadar tekrar seç
        while (prefabToDrop == null)
        {
            prefabToDrop = PickRandomCollectible();
        }

        colObj = Instantiate(prefabToDrop, spawnPos, Quaternion.identity);
    }

    private GameObject PickRandomCollectible()
    {
        List<GameObject> possibleDrops = new List<GameObject>();

        foreach (GameObject prefab in collectiblePrefab)
        {
            Collectible collectible = prefab.GetComponent<Collectible>();
            if (collectible == null || collectible.data == null) continue;

            float roll = Random.Range(0, 100);
            if (roll <= collectible.data.dropChance)
            {
                possibleDrops.Add(prefab);
            }
        }

        // Liste boşsa null döner, DropCollectible tekrar dener
        if (possibleDrops.Count == 0)
            return null;

        int randomIndex = Random.Range(0, possibleDrops.Count);
        return possibleDrops[randomIndex];
    }


}
