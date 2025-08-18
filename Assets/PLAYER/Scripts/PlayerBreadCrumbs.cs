using System.Collections.Generic;
using UnityEngine;

public class PlayerBreadcrumbs : MonoBehaviour
{
    [Header("Recording")]
    public float recordInterval = 0.3f;
    public float breadcrumbLifetime = 3f;
    public float minDistanceBetweenCrumbs = 0.12f;

    private float timer;

    public List<Vector2> Crumbs = new List<Vector2>();
    public List<float> Times = new List<float>();

    void Awake()
    {
        Crumbs.Clear();
        Times.Clear();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= recordInterval)
        {
            timer = 0f;

            Vector2 pos = transform.position;
            if (Crumbs.Count == 0 || Vector2.Distance(Crumbs[^1], pos) >= minDistanceBetweenCrumbs)
            {
                Crumbs.Add(pos);
                Times.Add(Time.time);
            }
        }

        // Yaşlı kırıntıları temizle
        while (Times.Count > 0 && Time.time - Times[0] > breadcrumbLifetime)
        {
            Times.RemoveAt(0);
            Crumbs.RemoveAt(0);
        }
    }

    // Enemy için yardımcı: sondan geriye doğru gez
    public Vector2? GetLastVisibleCrumb(Transform enemy, EnemyController controller)
    {
        for (int i = Crumbs.Count - 1; i >= 0; i--)
        {
            Vector2 crumbPos = Crumbs[i];
            if (controller.HasLineOfSight(crumbPos, false))
                return crumbPos;
        }
        return null; // Hiçbiri görünmüyor
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var crumb in Crumbs)
        {
            Gizmos.DrawSphere(crumb, 0.08f);
        }
    }
}
