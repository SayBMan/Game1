using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player etrafında kare (square) alan içinde örnek noktalar üretir.
/// - halfSize: karenin yarı uzunluğu (x ve y yönünde)
/// - spacing: nokta aralığı (mesh gibi)
/// - tüm noktaları GetAllPoints() ile döndürür (world-space)
/// </summary>
[RequireComponent(typeof(Transform))]
public class PlayerGridPoints : MonoBehaviour
{
    [Header("Grid settings")]
    [Tooltip("Karenin yarı-genişliği (Player merkezinden sağa/sola, yukarı/aşağı)")]
    public float halfSize = 1f;
    [Tooltip("Grid içindeki noktalar arası mesafe")]
    public float spacing = 0.3f;
    [Tooltip("Merkez (player pozisyonu) dahil edilsin mi?")]
    public bool includeCenter = true;

    // cache
    private List<Vector2> cachedPoints = new List<Vector2>();

    void Update()
    {
        RecalculateGrid();
    }

    public List<Vector2> GetAllPoints()
    {
        return cachedPoints;
    }

    private void RecalculateGrid()
    {
        cachedPoints.Clear();
        Vector2 center = transform.position;

        if (includeCenter)
            cachedPoints.Add(center);

        int stepsPerSide = Mathf.Max(1, Mathf.CeilToInt(halfSize * 2f / spacing));
        float startX = center.x - halfSize;
        float startY = center.y - halfSize;

        for (int i = 0; i <= stepsPerSide; i++)
        {
            for (int j = 0; j <= stepsPerSide; j++)
            {
                Vector2 p = new Vector2(startX + i * spacing, startY + j * spacing);
                // avoid duplicate center (if included)
                if (includeCenter && Vector2.Distance(p, center) < 0.001f) continue;
                // only include points inside the square (they are)
                cachedPoints.Add(p);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        RecalculateGrid();
        Gizmos.color = Color.yellow;
        foreach (var p in cachedPoints) Gizmos.DrawSphere(p, 0.04f);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(halfSize * 2f, halfSize * 2f, 0.01f));
    }
}
