using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))]
public class Piso3YSort : MonoBehaviour
{
    SpriteRenderer sr;
    void Awake(){sr=GetComponent<SpriteRenderer>();}
    void LateUpdate(){sr.sortingOrder=10000-Mathf.RoundToInt(transform.position.y*10);}
}
