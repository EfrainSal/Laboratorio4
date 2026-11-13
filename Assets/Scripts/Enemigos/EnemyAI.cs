using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float detectionRadius = 5f;

    [Header("Combat")]
    public float damage = 10f;

    [HideInInspector] public bool isKnockedBack = false; 

    private Transform player;
    private Rigidbody2D rb;
    private float zPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        zPosition = transform.position.z;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void FixedUpdate()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, zPosition);

        if (player == null) return;

        if (isKnockedBack) return; 

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * speed; 
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // ¡Esta es la función que faltaba para hacerte daño!
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
}