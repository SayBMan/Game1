using UnityEngine;

public class GameOverWatcher : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    bool shown;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.GetComponent<PlayerController>();
        }
    }

    void Update()
    {
        if (shown) return;
        if (player == null) return;
        if (player.isDead)
        {
            shown = true;
            GameOverUIController.Instance?.Show();
        }
    }
}
