using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Configuración del Spawner")]
    [Tooltip("Arrastra aquí los prefabs Enemy_Melee y Enemy_Ranged")]
    public GameObject[] enemyPrefabs; 
    public float activationRadius = 5f; 
    
    [Header("Oleadas y Cooldown")]
    public float cooldownTiempo = 10f; // Tiempo que tarda la trampa en reactivarse
    private float ultimoSpawnTiempo = -Mathf.Infinity; 

    [Header("Cantidad de Enemigos")]
    public int minEnemies = 1;
    public int maxEnemies = 3;
    public float spawnRadius = 1.8f; 
    [Tooltip("Evita colocar Sorbos dentro de muros y objetos sólidos del escenario.")]
    public bool evitarObstaculos;

    private Transform player;
    private bool spawnerRegistrado = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null || !this.enabled || DeathScreenController.GameplayBlocked) return;

        // Comprueba si ya pasó el tiempo de enfriamiento
        if (Time.time >= ultimoSpawnTiempo + cooldownTiempo)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= activationRadius)
            {
                SpawnEnemies();
            }
        }
    }

    private void SpawnEnemies()
    {
        ultimoSpawnTiempo = Time.time; // Reiniciamos el temporizador al momento actual

        // AVISAR AL LEVEL MANAGER (Solo la primera vez que se activa)
        if (!spawnerRegistrado && LevelManager.Instance != null)
        {
            LevelManager.Instance.RegistrarSpawnerActivado();
            spawnerRegistrado = true;
        }

        if (enemyPrefabs.Length == 0) return; // Evita errores si no asignas prefabs

        int enemiesToSpawn = Random.Range(minEnemies, maxEnemies + 1);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
            if (evitarObstaculos)
            {
                int intentos = 0;
                while (PosicionBloqueada(spawnPosition) && intentos++ < 24)
                    spawnPosition = transform.position + (Vector3)(Random.insideUnitCircle * spawnRadius);
                if (PosicionBloqueada(spawnPosition)) spawnPosition = transform.position;
                if (PosicionBloqueada(spawnPosition)) continue;
            }

            // Elegir aleatoriamente entre los prefabs asignados (Melee o Ranged)
            int randomIndex = Random.Range(0, enemyPrefabs.Length);
            GameObject prefabElegido = enemyPrefabs[randomIndex];

            var enemigo = Instantiate(prefabElegido, spawnPosition, Quaternion.identity);
            var salud = enemigo.GetComponent<EnemyHealth>();
            if (salud != null) salud.OrigenSpawner = this;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.RegistrarEnemigoSpawneado();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius); 
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius); 
    }

    private static bool PosicionBloqueada(Vector2 posicion)
    {
        foreach (var c in Physics2D.OverlapCircleAll(posicion, 0.3f))
            if (!c.isTrigger && c.GetComponentInParent<PlayerHealth>() == null
                && c.GetComponentInParent<EnemyHealth>() == null) return true;
        return false;
    }
}
