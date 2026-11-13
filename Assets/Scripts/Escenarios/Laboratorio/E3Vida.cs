using System;
using UnityEngine;

namespace Laboratorio4.Escenario3
{
    /// <summary>Vida de Jason y de los Sorbos. Parpadea en rojo al recibir daño.</summary>
    public class E3Vida : MonoBehaviour
    {
        public int maxima = 3;
        public int actual = 3;
        [Tooltip("Mientras está activo no recibe daño (escudo de Jason).")]
        public bool invulnerable = false;

        public event Action<int> AlRecibir;
        public event Action AlMorir;

        public bool Muerto { get { return actual <= 0; } }

        private SpriteRenderer sr;
        private float parpadeo;

        private void Awake()
        {
            actual = maxima;
            sr = GetComponent<SpriteRenderer>();
        }

        public bool Danio(int cantidad)
        {
            if (Muerto || invulnerable) return false;
            var puente = GetComponent<VidaJugadorPrototipo>();
            if (puente != null)
            {
                GetComponent<PlayerHealth>().TakeDamage(cantidad, DeathCause.Mutante);
                puente.Sincronizar(); parpadeo = .15f;
                AlRecibir?.Invoke(actual);
                if (actual <= 0) AlMorir?.Invoke();
                return true;
            }
            actual = Mathf.Max(0, actual - cantidad);
            parpadeo = 0.15f;
            if (AlRecibir != null) AlRecibir(actual);
            if (actual <= 0 && AlMorir != null) AlMorir();
            return true;
        }

        private void Update()
        {
            if (sr == null) return;
            if (parpadeo > 0f)
            {
                parpadeo -= Time.deltaTime;
                sr.color = new Color(1f, 0.35f, 0.35f, 1f);
            }
            else if (sr.color != Color.white) sr.color = Color.white;
        }
    }
}
