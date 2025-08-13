using UnityEngine;
using DG.Tweening;

public class ChestController : MonoBehaviour
{
    public GameObject collectiblePrefab;
    public Transform collectibleSpawnPoint;
    private Animator animator;
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
        Vector3 spawnPos = collectibleSpawnPoint != null
            ? collectibleSpawnPoint.position
            : transform.position + Vector3.up * 0.5f;

        // Collectible oluştur
        GameObject colObj = Instantiate(collectiblePrefab, spawnPos, Quaternion.identity);

        // Hedef: sandığın biraz altı
        Vector3 targetPos = transform.position + Vector3.down * 0.5f;

        // DOTween ile kavisli hareket
 
        colObj.transform.DOJump(targetPos, jumpPower, 1, jumpDuration)
            .SetEase(Ease.OutQuad);
        
    }

}
