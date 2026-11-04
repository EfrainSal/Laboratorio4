using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))]
public class Piso3SortByY : MonoBehaviour
{
    SpriteRenderer sr;
    void Awake(){sr=GetComponent<SpriteRenderer>();}
    void LateUpdate(){sr.sortingOrder=Mathf.RoundToInt(-transform.position.y*10);}
}
