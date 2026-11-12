using UnityEngine;

public class InvertCollider : MonoBehaviour
{
    [ContextMenu("Invert Points")]
    void InvertPoints()
    {
        EdgeCollider2D edgeCollider = GetComponent<EdgeCollider2D>();
        if (edgeCollider == null) return;

        Vector2[] points = edgeCollider.points;
        System.Array.Reverse(points);
        edgeCollider.points = points;
    }
}