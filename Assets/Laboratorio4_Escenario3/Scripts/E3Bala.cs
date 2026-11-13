using UnityEngine;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Bala por rayo: cada fotograma traza un rayo del tramo recorrido, así nunca atraviesa un muro
    /// aunque vaya muy rápido. El rayo va a la altura de los pies (donde están las colisiones 3/4);
    /// el dibujo se levanta 'alturaVisual' para que salga del arma.
    /// </summary>
    public class E3Bala : MonoBehaviour
    {
        public float velocidad = 14f;
        public float alcance = 12f;
        public int danio = 1;
        public float alturaVisual = 0.42f;

        private Vector2 pos, dir;
        private float recorrido;
        private Collider2D[] ignorar;
        private ContactFilter2D filtro;
        private readonly RaycastHit2D[] impactos = new RaycastHit2D[8];

        public void Iniciar(Vector2 inicio, Vector2 direccion, Collider2D[] ignorarColliders)
        {
            pos = inicio;
            dir = direccion.normalized;
            ignorar = ignorarColliders;
            filtro = new ContactFilter2D();
            filtro.useTriggers = false;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            Colocar();
        }

        private bool Ignorado(Collider2D c)
        {
            if (c == null) return true;
            if (ignorar != null) for (int i = 0; i < ignorar.Length; i++) if (ignorar[i] == c) return true;
            return false;
        }

        private void Update()
        {
            float paso = velocidad * Time.deltaTime;
            int n = Physics2D.Raycast(pos, dir, filtro, impactos, paso);
            float mejor = float.MaxValue;
            Collider2D golpeado = null;
            for (int i = 0; i < n; i++)
            {
                if (Ignorado(impactos[i].collider)) continue;
                if (impactos[i].distance < mejor) { mejor = impactos[i].distance; golpeado = impactos[i].collider; }
            }
            if (golpeado != null)
            {
                E3Vida v = golpeado.GetComponentInParent<E3Vida>();
                if (v != null && !v.Muerto) v.Danio(danio);
                Destroy(gameObject);
                return;
            }
            pos += dir * paso;
            recorrido += paso;
            if (recorrido > alcance) { Destroy(gameObject); return; }
            Colocar();
        }

        private void Colocar()
        {
            transform.position = new Vector3(pos.x, pos.y + alturaVisual, 0f);
        }
    }
}
