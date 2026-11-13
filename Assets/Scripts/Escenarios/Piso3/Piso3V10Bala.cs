using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Bala con "barrido": en cada frame lanza un rayo desde su posición anterior hasta la nueva,
    /// así que NUNCA atraviesa un muro aunque vaya muy rápido (antes, con muros sin grosor, las
    /// balas pasaban de largo en algunas paredes).
    ///
    /// Perspectiva 3/4: la bala viaja por el SUELO (a la altura de los pies, donde están las
    /// colisiones de muros y objetos) y se DIBUJA "altura" unidades más arriba, a la altura del arma.
    /// Así choca con la base del muro / objeto y visualmente impacta en su cara.
    /// </summary>
    public class Piso3V10Bala : MonoBehaviour
    {
        public float velocidad = 14f;
        public float vidaMaxima = 1.6f;
        public float altura = 0.33f;
        public Sprite spriteImpacto;

        private Vector2 suelo;
        private Vector2 direccion;
        private float vida;
        private Collider2D[] ignorar;
        private SpriteRenderer sr;
        private ContactFilter2D filtro;
        private static readonly RaycastHit2D[] impactos = new RaycastHit2D[16];

        /// <summary>Crea y dispara una bala. pies = posición del tirador; bocaSuelo = boca del arma proyectada al suelo.</summary>
        public static Piso3V10Bala Disparar(Vector2 pies, Vector2 bocaSuelo, float altura, Vector2 direccion,
                                            Sprite spriteBala, Sprite spriteImpacto, float velocidad, Collider2D[] ignorar)
        {
            GameObject go = new GameObject("Bala");
            Piso3V10Bala b = go.AddComponent<Piso3V10Bala>();
            b.sr = go.AddComponent<SpriteRenderer>();
            b.sr.sprite = spriteBala;
            b.spriteImpacto = spriteImpacto;
            b.velocidad = velocidad;
            b.altura = altura;
            b.direccion = direccion.sqrMagnitude > 0f ? direccion.normalized : Vector2.right;
            b.ignorar = ignorar;
            b.filtro = new ContactFilter2D();
            b.filtro.useTriggers = false;
            float ang = Mathf.Atan2(b.direccion.y, b.direccion.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, ang);

            // primer tramo: de los pies a la boca del arma (si hay un muro pegado, choca ahí)
            b.suelo = pies;
            Vector2 tramo = bocaSuelo - pies;
            if (!b.Avanzar(tramo.normalized, tramo.magnitude))
                b.Colocar();
            return b;
        }

        private void Update()
        {
            if (Avanzar(direccion, velocidad * Time.deltaTime)) return;
            Colocar();
            vida += Time.deltaTime;
            if (vida >= vidaMaxima) Destroy(gameObject);
        }

        /// <summary>Mueve la bala por el suelo; devuelve true si chocó (y ya se destruyó).</summary>
        private bool Avanzar(Vector2 dir, float dist)
        {
            if (dist <= 0f) return false;
            int n = Physics2D.Raycast(suelo, dir, filtro, impactos, dist);
            float mejor = float.MaxValue;
            RaycastHit2D elegido = default(RaycastHit2D);
            for (int i = 0; i < n; i++)
            {
                RaycastHit2D h = impactos[i];
                if (h.collider == null || EsIgnorado(h.collider)) continue;
                if (h.distance < mejor) { mejor = h.distance; elegido = h; }
            }
            if (mejor == float.MaxValue)
            {
                suelo += dir * dist;
                return false;
            }
            Impactar(elegido);
            return true;
        }

        private bool EsIgnorado(Collider2D c)
        {
            if (ignorar == null) return false;
            for (int i = 0; i < ignorar.Length; i++) if (ignorar[i] == c) return true;
            return false;
        }

        private void Colocar()
        {
            transform.position = new Vector3(suelo.x, suelo.y + altura, 0f);
            if (sr != null) sr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-suelo.y * 100f) + 2, -29000, 29000);
        }

        private void Impactar(RaycastHit2D h)
        {
            Piso3V10Zorbo z = h.collider.GetComponentInParent<Piso3V10Zorbo>();
            if (z != null) z.RecibirImpacto();

            if (spriteImpacto != null)
            {
                GameObject fx = new GameObject("Impacto");
                fx.transform.position = new Vector3(h.point.x, h.point.y + altura, 0f);
                SpriteRenderer fsr = fx.AddComponent<SpriteRenderer>();
                fsr.sprite = spriteImpacto;
                fsr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-h.point.y * 100f) + 3, -29000, 29000);
                Destroy(fx, 0.08f);
            }
            Destroy(gameObject);
        }
    }
}
