using System.Collections;
using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Sorbo (dos variantes: normal y acorazado). Se queda en su sitio:
    /// - Reposo: mira en su orientación inicial (respira levemente).
    /// - Si Jason se acerca a menos de 'rangoAtaque', se gira hacia él y reproduce su ataque
    ///   (todavía no hace daño: no existe sistema de vida).
    /// - Recibe disparos: parpadea en rojo; con 'vida' impactos reproduce su muerte y deja de bloquear.
    /// Orientación: 0 Norte, 1 Noreste, 2 Este, 3 Sureste, 4 Sur, 5 Suroeste, 6 Oeste, 7 Noroeste.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SorboEnemigo : MonoBehaviour
    {
        public string variante = "Sorbo";
        [Range(0, 7)] public int orientacion = 4;
        public int vida = 3;
        public float rangoAtaque = 1.3f;

        [Header("Reposo: 8 direcciones (si está vacío se usan las 4 de abajo)")]
        public Sprite[] quieto8 = new Sprite[0];
        [Tooltip("Reposo en 4 direcciones (Norte, Este, Sur, Oeste)")]
        public Sprite[] quieto4 = new Sprite[0];

        [Header("Ataque y muerte (4 direcciones)")]
        public Sprite[] ataqueNorte, ataqueEste, ataqueSur, ataqueOeste;
        public Sprite[] muerteNorte, muerteEste, muerteSur, muerteOeste;
        public float fpsAtaque = 12f, fpsMuerte = 10f;

        SpriteRenderer sr;
        Transform jugador;
        Vector3 escalaBase;
        float fase, reloj;
        Sprite[] anim; bool muerto;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            escalaBase = transform.localScale;
            fase = Random.value * 10f;
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j != null) jugador = j.transform;
            Reposo();
        }

        void OnValidate() { if (!Application.isPlaying) { sr = GetComponent<SpriteRenderer>(); Reposo(); } }

        void Update()
        {
            if (muerto)
            {
                reloj += Time.deltaTime;
                if (anim != null && anim.Length > 0) sr.sprite = anim[Mathf.Min(Mathf.FloorToInt(reloj * fpsMuerte), anim.Length - 1)];
                return;
            }
            if (jugador == null || !jugador.gameObject.activeInHierarchy)
            {
                var j = GameObject.FindGameObjectWithTag("Player");
                jugador = j != null ? j.transform : null;
            }
            Vector2 d = jugador != null ? (Vector2)(jugador.position - transform.position) : Vector2.zero;
            if (jugador != null && d.magnitude < rangoAtaque)
            {
                anim = Por4(d, ataqueNorte, ataqueEste, ataqueSur, ataqueOeste);
                reloj += Time.deltaTime;
                if (anim != null && anim.Length > 0) { sr.sprite = anim[Mathf.FloorToInt(reloj * fpsAtaque) % anim.Length]; return; }
            }
            reloj = 0f;
            Reposo();
            float s = 1f + Mathf.Sin((Time.time + fase) * 2.2f) * 0.02f;
            transform.localScale = new Vector3(escalaBase.x, escalaBase.y * s, escalaBase.z);
        }

        void Reposo()
        {
            if (sr == null) return;
            if (quieto8 != null && quieto8.Length == 8 && quieto8[orientacion] != null) { sr.sprite = quieto8[orientacion]; return; }
            if (quieto4 != null && quieto4.Length == 4)
            {
                int i = orientacion == 0 || orientacion == 1 || orientacion == 7 ? 0 : orientacion == 2 || orientacion == 3 ? 1 : orientacion == 4 ? 2 : 3;
                if (quieto4[i] != null) sr.sprite = quieto4[i];
            }
        }

        static Sprite[] Por4(Vector2 d, Sprite[] n, Sprite[] e, Sprite[] s, Sprite[] o)
        {
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? e : o;
            return d.y > 0 ? n : s;
        }

        public void Golpear()
        {
            if (muerto || !isActiveAndEnabled) return;
            vida--;
            if (vida <= 0) { Morir(); return; }
            StartCoroutine(Parpadeo());
        }

        void Morir()
        {
            muerto = true;
            reloj = 0f;
            transform.localScale = escalaBase;
            Vector2 d = jugador != null ? (Vector2)(jugador.position - transform.position) : Vector2.down;
            anim = Por4(d, muerteNorte, muerteEste, muerteSur, muerteOeste);
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            var sombra = transform.Find("Sombra");
            if (sombra != null) sombra.gameObject.SetActive(false);
        }

        IEnumerator Parpadeo()
        {
            for (int i = 0; i < 2; i++)
            {
                sr.color = new Color(1f, 0.25f, 0.25f);
                yield return new WaitForSeconds(0.06f);
                sr.color = Color.white;
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}
