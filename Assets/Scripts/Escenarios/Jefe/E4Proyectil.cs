using System.Collections.Generic;
using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>
    /// Bala de Jason/Milena o bola de poder del Doctor. Avanza a la altura del suelo y revisa todo su
    /// recorrido en cada fotograma (CircleCast), así que no atraviesa muros, columnas ni objetos.
    /// </summary>
    public class E4Proyectil : MonoBehaviour
    {
        public E4Bando bando = E4Bando.Jugadores;
        public int danio = 1;
        public float velocidad = 12f;
        public float radio = 0.1f;
        public float vidaMax = 2.5f;
        public Vector2 dir = Vector2.up;
        public Sprite impacto;
        public float altura = 0.4f;
        public Transform ignorar;
        public Transform visual;
        [Tooltip("Pulso de tamaño del dibujo (bola de poder).")]
        public bool pulsar = false;

        private static readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        private ContactFilter2D filtro;
        private float t;

        private void Awake()
        {
            filtro = new ContactFilter2D();
            filtro.useTriggers = true;
            filtro.useLayerMask = false;
        }

        private void Update()
        {
            float paso = velocidad * Time.deltaTime;
            Vector2 p = transform.position;
            hits.Clear();
            Physics2D.CircleCast(p, radio, dir, filtro, hits, paso);
            hits.Sort((a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Count; i++)
            {
                Collider2D c = hits[i].collider;
                if (c == null) continue;
                if (ignorar != null && c.transform.IsChildOf(ignorar)) continue;
                if (c.GetComponentInParent<E4Proyectil>() != null) continue;
                if (c.isTrigger)
                {
                    E4Hurtbox hb = c.GetComponent<E4Hurtbox>();
                    if (hb != null && hb.vida != null && hb.vida.bando != bando && !hb.vida.Muerto)
                    {
                        Golpear(hb.vida, hits[i].point);
                        return;
                    }
                    continue;
                }
                E4Vida v = c.GetComponentInParent<E4Vida>();
                if (v != null)
                {
                    if (v.bando == bando || v.Muerto) continue;
                    Golpear(v, hits[i].point);
                    return;
                }
                Impacto(hits[i].point);   // muro, columna, objeto o puerta
                return;
            }
            transform.position = p + dir * paso;
            if (pulsar && visual != null)
            {
                float s = 1f + Mathf.Sin(Time.time * 18f) * 0.12f;
                visual.localScale = new Vector3(s, s, 1f);
            }
            t += Time.deltaTime;
            if (t > vidaMax) Destroy(gameObject);
        }

        private void Golpear(E4Vida v, Vector2 punto)
        {
            v.Recibir(danio);   // si tiene el escudo activo no hace daño: la bola se deshace igual
            Impacto(punto);
        }

        private void Impacto(Vector2 punto)
        {
            if (impacto != null)
            {
                GameObject go = new GameObject("E4_Impacto");
                go.transform.position = new Vector3(punto.x, punto.y + altura, 0f);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = impacto;
                sr.sortingOrder = Mathf.RoundToInt(-punto.y * 100f) + 5;
                Destroy(go, 0.12f);
            }
            Destroy(gameObject);
        }
    }
}
