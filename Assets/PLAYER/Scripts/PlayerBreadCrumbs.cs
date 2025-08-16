using System.Collections.Generic;
using UnityEngine;

public class PlayerBreadcrumbs : MonoBehaviour
{
    [Header("Recording")]
    public float recordInterval = 0.3f;      // kaç sn’de bir konum kaydı
    public float breadcrumbLifetime = 3f;    // en fazla kaç sn’lik iz tutulacak
    public float minDistanceBetweenCrumbs = 0.12f; // peş peşe çok yakın noktaları alma

    private float timer;

    // Statik listeler: Enemy’ler buradan okuyacak
    public static readonly List<Vector3> Crumbs = new List<Vector3>();
    public static readonly List<float> Times = new List<float>();

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

            Vector3 pos = transform.position;
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var crumb in Crumbs)
        {
            Gizmos.DrawSphere(crumb, 0.1f);
        }
    }
}
