using UnityEngine;
public class Piso3Marker : MonoBehaviour
{
    public Color markerColor=Color.yellow;
    void OnDrawGizmos(){Gizmos.color=markerColor;Gizmos.DrawWireSphere(transform.position,.55f);}
}
