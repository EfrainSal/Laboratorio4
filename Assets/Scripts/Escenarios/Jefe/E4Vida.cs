using System;
using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>Vida de un personaje. Parpadea en rojo al recibir daño.</summary>
    public class E4Vida : MonoBehaviour
    {
        public E4Bando bando = E4Bando.Jugadores;
        public int maximo = 10;
        public int actual = 10;
        [Tooltip("Con el escudo activo no recibe daño.")]
        public bool invulnerable = false;

        public event Action AlMorir;
        public event Action<int> AlRecibir;

        public bool Muerto { get { return actual <= 0; } }
        public float Fraccion { get { return maximo > 0 ? (float)actual / maximo : 0f; } }

        private SpriteRenderer sr;
        private float tParpadeo;

        private void Awake()
        {
            actual = maximo;
            sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>Devuelve true si el daño se aplicó (false si estaba muerto o con escudo).</summary>
        public bool Recibir(int danio)
        {
            if (Muerto || invulnerable || danio <= 0) return false;
            var puente = GetComponent<VidaJugadorPrototipo>();
            if (puente != null)
            {
                GetComponent<PlayerHealth>().TakeDamage(danio, DeathCause.Jefe);
                puente.Sincronizar(); tParpadeo = .14f;
                AlRecibir?.Invoke(danio);
                if (actual <= 0) AlMorir?.Invoke();
                return true;
            }
            actual = Mathf.Max(0, actual - danio);
            tParpadeo = 0.14f;
            if (AlRecibir != null) AlRecibir(danio);
            if (actual == 0 && AlMorir != null) AlMorir();
            return true;
        }

        private void Update()
        {
            if (sr == null) return;
            if (tParpadeo > 0f)
            {
                tParpadeo -= Time.deltaTime;
                sr.color = tParpadeo > 0f ? new Color(1f, 0.35f, 0.35f, 1f) : Color.white;
            }
        }
    }
}
