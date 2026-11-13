using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// "Barrido y deslizamiento" compartido por Jason y los Zorbos.
    /// Lanza la cápsula en la dirección del movimiento, se detiene a "piel" de lo que encuentre y
    /// desliza el resto a lo largo de la superficie. El cuerpo nunca entra en un collider:
    /// no atraviesa muros, objetos ni personajes y no vibra en las esquinas.
    /// </summary>
    public static class Piso3V10Barrido
    {
        private static readonly RaycastHit2D[] impactos = new RaycastHit2D[24];
        private static readonly Collider2D[] solapes = new Collider2D[24];

        public static ContactFilter2D Filtro(GameObject go)
        {
            ContactFilter2D f = new ContactFilter2D();
            f.useTriggers = false;
            f.SetLayerMask(Physics2D.GetLayerCollisionMask(go.layer));
            return f;
        }

        /// <summary>
        /// V15.2: distancia mínima real a paredes. La física 2D (Box2D) deja los barridos ~0.015 DENTRO del
        /// borde de la pared; con 0.01 el personaje quedaba rozando: la separación lo sacaba y el siguiente
        /// paso lo volvía a meter (vibraba) y a veces el barrido arrancaba "tocando" y lo bloqueaba en
        /// todas direcciones (se trababa en bordes y esquinas). Con 0.03 nunca queda rozando.
        /// </summary>
        private const float PielMinima = 0.03f;
        private const float CosMinimo = 0.25f;
        private static readonly float[] Rodeos = { 0.04f, 0.08f, 0.12f, 0.16f, 0.20f };

        /// <summary>Devuelve la nueva posición del cuerpo tras intentar moverse "desplazamiento".</summary>
        public static Vector2 Mover(Rigidbody2D cuerpo, CapsuleCollider2D capsula, ContactFilter2D filtro,
                                    Vector2 desplazamiento, float piel)
        {
            piel = Mathf.Max(piel, PielMinima);
            Vector2 escala = capsula.transform.lossyScale;
            Vector2 tam = new Vector2(capsula.size.x * Mathf.Abs(escala.x), capsula.size.y * Mathf.Abs(escala.y));
            Vector2 desfase = new Vector2(capsula.offset.x * escala.x, capsula.offset.y * escala.y);

            Vector2 inicio = Separar(cuerpo, capsula, filtro, cuerpo.position);
            float largo = desplazamiento.magnitude;
            if (largo <= 0.00001f) return inicio;
            Vector2 dv = desplazamiento / largo;

            Vector2 pos = Barrer(cuerpo, capsula, filtro, inicio, desplazamiento, piel, tam, desfase);

            // Esquinas y bordes de pocos píxeles: si casi no avanzó yendo recto (teclado), busca un rodeo
            // lateral de hasta 0.2 para "resbalar" por la esquina en vez de quedarse pegado.
            if (Vector2.Dot(pos - inicio, dv) < 0.25f * largo && Mathf.Max(Mathf.Abs(dv.x), Mathf.Abs(dv.y)) > 0.9f)
            {
                Vector2 perp = new Vector2(-dv.y, dv.x);
                bool hecho = false;
                for (int k = 0; k < Rodeos.Length && !hecho; k++)
                    for (int lado = 1; lado >= -1 && !hecho; lado -= 2)
                    {
                        Vector2 pd = perp * lado;
                        float r = Rodeos[k];
                        if (!Libre(cuerpo, capsula, filtro, inicio, pd, r, piel, tam, desfase)) continue;
                        if (!Libre(cuerpo, capsula, filtro, inicio + pd * r, dv, 0.1f, piel, tam, desfase)) continue;
                        Vector2 q = inicio + pd * Mathf.Min(r, largo);
                        if (largo > r) q += dv * (largo - r);
                        pos = q;
                        hecho = true;
                    }
            }

            // Red de seguridad: si el paso terminara DENTRO de una pared/objeto y antes no lo estaba, no se da.
            if (Solapa(cuerpo, capsula, filtro, pos + desfase, tam) && !Solapa(cuerpo, capsula, filtro, inicio + desfase, tam))
                return inicio;
            return pos;
        }

        /// <summary>Barrido y deslizamiento (hasta 4 tramos), dejando "piel" de distancia real a lo que toque.</summary>
        private static Vector2 Barrer(Rigidbody2D cuerpo, CapsuleCollider2D capsula, ContactFilter2D filtro,
                                      Vector2 pos, Vector2 resto, float piel, Vector2 tam, Vector2 desfase)
        {
            Vector2 normalPrevia = Vector2.zero;
            for (int iter = 0; iter < 4; iter++)
            {
                float dist = resto.magnitude;
                if (dist < 0.00001f) break;
                Vector2 dir = resto / dist;

                float mejor;
                Vector2 normal;
                if (!Choque(cuerpo, capsula, filtro, pos, dir, dist + piel / CosMinimo, tam, desfase, iter == 0, out mejor, out normal))
                {
                    pos += resto;
                    break;
                }
                float cos = Mathf.Max(-Vector2.Dot(normal, dir), CosMinimo);
                float avance = Mathf.Clamp(mejor - piel / cos, 0f, dist);
                pos += dir * avance;
                resto -= dir * avance;
                resto -= Vector2.Dot(resto, normal) * normal;
                // esquina interior: si al deslizar por la 2ª pared se volvería a meter en la 1ª, se para.
                if (iter > 0 && Vector2.Dot(resto, normalPrevia) < 0f) resto = Vector2.zero;
                normalPrevia = normal;
            }
            return pos;
        }

        /// <summary>Primer obstáculo en la dirección "dir" (distancia y normal). false si el camino está libre.</summary>
        private static bool Choque(Rigidbody2D cuerpo, CapsuleCollider2D capsula, ContactFilter2D filtro, Vector2 pos, Vector2 dir,
                                   float dist, Vector2 tam, Vector2 desfase, bool poseReal, out float mejor, out Vector2 normal)
        {
            mejor = float.MaxValue;
            normal = Vector2.zero;
            int n = Physics2D.CapsuleCast(pos + desfase, tam, capsula.direction, 0f, dir, filtro, impactos, dist);
            for (int i = 0; i < n; i++)
            {
                RaycastHit2D h = impactos[i];
                if (h.collider == null || h.collider == capsula || h.collider.attachedRigidbody == cuerpo) continue;
                Vector2 nn = h.normal;
                if (h.distance <= 0.0001f && poseReal)
                {
                    // Empieza tocando: Unity devuelve normal = -dir (bloquearía en TODAS direcciones).
                    // Se usa la normal real de contacto; si te alejas o vas en paralelo, no bloquea.
                    ColliderDistance2D d = capsula.Distance(h.collider);
                    if (d.isValid)
                    {
                        Vector2 v = d.distance >= 0f ? d.pointA - d.pointB : d.pointB - d.pointA;
                        if (v.sqrMagnitude > 1e-10f) nn = v.normalized;
                    }
                }
                if (Vector2.Dot(nn, dir) >= 0f) continue;
                if (h.distance < mejor) { mejor = h.distance; normal = nn; }
            }
            return mejor < float.MaxValue;
        }

        /// <summary>¿Se puede avanzar "dist" desde "o" en "dir" dejando la piel?</summary>
        private static bool Libre(Rigidbody2D cuerpo, CapsuleCollider2D capsula, ContactFilter2D filtro, Vector2 o, Vector2 dir,
                                  float dist, float piel, Vector2 tam, Vector2 desfase)
        {
            float mejor;
            Vector2 normal;
            if (!Choque(cuerpo, capsula, filtro, o, dir, dist + piel / CosMinimo, tam, desfase, false, out mejor, out normal)) return true;
            float cos = Mathf.Max(-Vector2.Dot(normal, dir), CosMinimo);
            return mejor - piel / cos >= dist;
        }

        /// <summary>¿La cápsula (reducida 0.03 por lado) en ese centro se mete en algo sólido que no sea ella?</summary>
        private static bool Solapa(Rigidbody2D cuerpo, CapsuleCollider2D capsula, ContactFilter2D filtro, Vector2 centro, Vector2 tam)
        {
            Vector2 t = new Vector2(Mathf.Max(0.02f, tam.x - 0.06f), Mathf.Max(0.02f, tam.y - 0.06f));
            int n = Physics2D.OverlapCapsule(centro, t, capsula.direction, 0f, filtro, solapes);
            for (int i = 0; i < n; i++)
            {
                Collider2D c = solapes[i];
                if (c == null || c == capsula || c.attachedRigidbody == cuerpo) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Si empieza metido en algo (aparición, otro personaje encima), lo saca.
        /// Con capsula.Distance(otro): pointA está en la cápsula y pointB en el otro; para salir hay que
        /// mover la cápsula pointB - pointA (V15.1). V15.2: ignora roces de menos de 0.004 (evita vibrar).
        /// </summary>
        private static Vector2 Separar(Rigidbody2D cuerpo, CapsuleCollider2D capsula, ContactFilter2D filtro, Vector2 pos)
        {
            int n = capsula.Overlap(filtro, solapes);
            Vector2 empuje = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                Collider2D c = solapes[i];
                if (c == null || c == capsula || c.attachedRigidbody == cuerpo) continue;
                ColliderDistance2D d = capsula.Distance(c);
                if (!d.isValid || !d.isOverlapped || d.distance > -0.004f) continue;
                Vector2 salida = d.pointB - d.pointA;
                if (salida.sqrMagnitude < 1e-10f) continue;
                if (Mathf.Abs(salida.x) > Mathf.Abs(empuje.x)) empuje.x = salida.x;
                if (Mathf.Abs(salida.y) > Mathf.Abs(empuje.y)) empuje.y = salida.y;
            }
            return pos + Vector2.ClampMagnitude(empuje, 0.3f);
        }

        /// <summary>
        /// V16 – Diagnóstico de atascos: escribe en la Consola qué tiene delante y encima el personaje que no avanza.
        /// </summary>
        public static void Diagnosticar(string quien, Rigidbody2D cuerpo, CapsuleCollider2D capsula, ContactFilter2D filtro, Vector2 pos, Vector2 dir)
        {
            Vector2 escala = capsula.transform.lossyScale;
            Vector2 tam = new Vector2(capsula.size.x * Mathf.Abs(escala.x), capsula.size.y * Mathf.Abs(escala.y));
            Vector2 desfase = new Vector2(capsula.offset.x * escala.x, capsula.offset.y * escala.y);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("[Piso3 Atasco] ").Append(quien).Append(" en ").Append(pos.ToString("F2")).Append(" quiere ir hacia ")
              .Append(dir.ToString("F2")).Append(" y no avanza. Delante (0.3): ");
            int n = Physics2D.CapsuleCast(pos + desfase, tam, capsula.direction, 0f, dir, filtro, impactos, 0.3f);
            int k = 0;
            for (int i = 0; i < n && k < 4; i++)
            {
                RaycastHit2D h = impactos[i];
                if (h.collider == null || h.collider == capsula || h.collider.attachedRigidbody == cuerpo) continue;
                sb.Append("'").Append(Nombre(h.collider)).Append("' a ").Append(h.distance.ToString("F3"))
                  .Append(" normal ").Append(h.normal.ToString("F2")).Append("; ");
                k++;
            }
            if (k == 0) sb.Append("nada; ");
            sb.Append("Tocando ahora: ");
            int m = capsula.Overlap(filtro, solapes);
            k = 0;
            for (int i = 0; i < m && k < 4; i++)
            {
                Collider2D c = solapes[i];
                if (c == null || c == capsula || c.attachedRigidbody == cuerpo) continue;
                ColliderDistance2D d = capsula.Distance(c);
                sb.Append("'").Append(Nombre(c)).Append("' dist ").Append(d.distance.ToString("F3")).Append("; ");
                k++;
            }
            if (k == 0) sb.Append("nada.");
            sb.Append(" (Manda esta línea si ahí no se ve pared.)");
            Debug.LogWarning(sb.ToString(), capsula);
        }

        private static string Nombre(Collider2D c)
        {
            string n = c.GetType().Name + " " + c.name;
            if (c.transform.parent != null) n = c.transform.parent.name + "/" + n;
            return n;
        }

        /// <summary>¿Hay línea de visión (sin muros ni objetos) entre dos puntos del suelo?</summary>
        public static bool LineaDeVision(Vector2 desde, Vector2 hasta, Collider2D[] ignorar, Transform objetivo)
        {
            ContactFilter2D f = new ContactFilter2D();
            f.useTriggers = false;
            Vector2 d = hasta - desde;
            float dist = d.magnitude;
            if (dist < 0.0001f) return true;
            int n = Physics2D.Raycast(desde, d / dist, f, impactos, dist);
            for (int i = 0; i < n; i++)
            {
                Collider2D c = impactos[i].collider;
                if (c == null) continue;
                bool ign = false;
                if (ignorar != null) foreach (Collider2D x in ignorar) if (x == c) { ign = true; break; }
                if (ign) continue;
                if (objetivo != null && c.transform.IsChildOf(objetivo)) continue;
                if (c.GetComponentInParent<Piso3V10Zorbo>() != null) continue;   // otros Zorbos no tapan la vista
                return false;
            }
            return true;
        }
    }
}
