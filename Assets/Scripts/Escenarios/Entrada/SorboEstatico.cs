using System.Collections;
using UnityEngine;

namespace Laboratorio4
{
    /// <summary>Sorbo estático: respira levemente, bloquea el paso y parpadea en rojo al recibir un disparo.</summary>
    public class SorboEstatico : MonoBehaviour
    {
        public float amplitudRespiracion = 0.02f;
        public float velocidadRespiracion = 2.2f;
        SpriteRenderer sr;
        Vector3 escalaBase;
        float fase;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            escalaBase = transform.localScale;
            fase = Random.value * 10f;
        }

        void Update()
        {
            float s = 1f + Mathf.Sin((Time.time + fase) * velocidadRespiracion) * amplitudRespiracion;
            transform.localScale = new Vector3(escalaBase.x, escalaBase.y * s, escalaBase.z);
        }

        public void Golpear()
        {
            if (sr != null && isActiveAndEnabled) StartCoroutine(Parpadeo());
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
