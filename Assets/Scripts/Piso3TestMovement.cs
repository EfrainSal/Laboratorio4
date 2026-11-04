using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class Piso3TestMovement : MonoBehaviour
{
    public float speed = 5f;
    Rigidbody2D rb;
    Vector2 direction;
    void Awake(){rb=GetComponent<Rigidbody2D>();rb.gravityScale=0;rb.freezeRotation=true;}
    void Update()
    {
        // Compatible with the legacy Input Manager; select Both in Player Settings.
        direction=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")).normalized;
    }
    void FixedUpdate(){rb.MovePosition(rb.position+direction*speed*Time.fixedDeltaTime);}
}
