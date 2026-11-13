using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class Piso3TestPlayer : MonoBehaviour
{
    public float speed=5f;
    Rigidbody2D rb; Vector2 input;
    void Awake(){rb=GetComponent<Rigidbody2D>();rb.gravityScale=0;rb.freezeRotation=true;}
    void Update(){input=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")).normalized;}
    void FixedUpdate(){rb.MovePosition(rb.position+input*speed*Time.fixedDeltaTime);}
    void LateUpdate(){var sr=GetComponent<SpriteRenderer>();if(sr)sr.sortingOrder=Mathf.RoundToInt(-transform.position.y*10);}
}
