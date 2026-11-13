using System.Collections.Generic;
using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// QA: tecla C muestra/oculta los colliders reales de la escena (rojo = muros sólidos,
    /// verde = objetos, amarillo = bloqueo de puertas, magenta = Zorbos/personajes, cian = jugador).
    /// Tecla R devuelve al jugador a su posición inicial.
    /// </summary>
    public class Piso3V10Depuracion : MonoBehaviour
    {
        public Piso3V10Movimiento jugador;
        public bool visibleAlIniciar = false;

        private readonly List<LineRenderer> lineas = new List<LineRenderer>();
        private Transform contenedor;
        private bool visible;
        private Material material;

        private void Start()
        {
            visible = visibleAlIniciar;
            if (visible) Construir();
        }

        private void Update()
        {
            if (Piso3V10Entrada.PulsoDepuracion())
            {
                visible = !visible;
                if (visible) Construir();
                else Limpiar();
            }
            if (Piso3V10Entrada.PulsoReinicio() && jugador != null && jugador.isActiveAndEnabled)
            {
                jugador.Reiniciar();
            }
            if (visible && jugador != null) ActualizarJugador();
        }

        private Material Mat()
        {
            if (material != null) return material;
            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s == null) s = Shader.Find("Sprites/Default");
            material = new Material(s);
            return material;
        }

        private LineRenderer NuevaLinea(Color c, string nombre)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(contenedor, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mat();
            lr.startColor = c; lr.endColor = c;
            lr.startWidth = 0.035f; lr.endWidth = 0.035f;
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.sortingOrder = 30000;
            lineas.Add(lr);
            return lr;
        }

        private void Construir()
        {
            Limpiar();
            contenedor = new GameObject("_DepuracionColliders").transform;

            foreach (EdgeCollider2D e in FindObjectsByType<EdgeCollider2D>(FindObjectsSortMode.None))
            {
                Vector2[] p = e.points;
                LineRenderer lr = NuevaLinea(new Color(1f, 0.15f, 0.15f), "Muro");
                lr.positionCount = p.Length;
                for (int i = 0; i < p.Length; i++)
                    lr.SetPosition(i, e.transform.TransformPoint(p[i] + e.offset));
            }

            foreach (PolygonCollider2D pc in FindObjectsByType<PolygonCollider2D>(FindObjectsSortMode.None))
            {
                for (int i = 0; i < pc.pathCount; i++)
                {
                    Vector2[] p = pc.GetPath(i);
                    LineRenderer lr = NuevaLinea(new Color(1f, 0.15f, 0.15f), "MuroSolido");
                    lr.loop = true;
                    lr.positionCount = p.Length;
                    for (int k = 0; k < p.Length; k++) lr.SetPosition(k, pc.transform.TransformPoint(p[k] + pc.offset));
                }
            }

            foreach (CompositeCollider2D cc in FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None))
            {
                for (int i = 0; i < cc.pathCount; i++)
                {
                    Vector2[] p = new Vector2[cc.GetPathPointCount(i)];
                    cc.GetPath(i, p);
                    LineRenderer lr = NuevaLinea(new Color(1f, 0.15f, 0.15f), "MuroSolido");
                    lr.loop = true;
                    lr.positionCount = p.Length;
                    for (int k = 0; k < p.Length; k++) lr.SetPosition(k, cc.transform.TransformPoint(p[k]));
                }
            }

            foreach (CapsuleCollider2D c in FindObjectsByType<CapsuleCollider2D>(FindObjectsSortMode.None))
            {
                if (jugador != null && c.gameObject == jugador.gameObject) continue;
                LineRenderer lr = NuevaLinea(new Color(1f, 0.3f, 1f), "Personaje");
                DibujarCaja(lr, c.transform, c.offset, c.size);
            }

            foreach (BoxCollider2D b in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            {
                if (!b.enabled) continue;
                bool esPuerta = b.GetComponentInParent<Piso3V10Puerta>() != null;
                bool esMuro = b.GetComponentInParent<Piso3V10Colisiones>() != null;
                LineRenderer lr = NuevaLinea(esMuro ? new Color(1f, 0.15f, 0.15f) : esPuerta ? new Color(1f, 0.85f, 0f) : new Color(0.2f, 1f, 0.45f),
                                             esMuro ? "MuroSolido" : "Caja");
                DibujarCaja(lr, b.transform, b.offset, b.size);
            }
        }

        private void ActualizarJugador()
        {
            CapsuleCollider2D cap = jugador.GetComponent<CapsuleCollider2D>();
            if (cap == null) return;
            LineRenderer lr = null;
            foreach (LineRenderer l in lineas) if (l != null && l.name == "Jugador") lr = l;
            if (lr == null) lr = NuevaLinea(new Color(0.2f, 0.9f, 1f), "Jugador");
            DibujarCaja(lr, cap.transform, cap.offset, cap.size);
        }

        private static void DibujarCaja(LineRenderer lr, Transform t, Vector2 off, Vector2 size)
        {
            Vector2 h = size * 0.5f;
            Vector3[] c =
            {
                t.TransformPoint(off + new Vector2(-h.x, -h.y)),
                t.TransformPoint(off + new Vector2(-h.x,  h.y)),
                t.TransformPoint(off + new Vector2( h.x,  h.y)),
                t.TransformPoint(off + new Vector2( h.x, -h.y)),
                t.TransformPoint(off + new Vector2(-h.x, -h.y)),
            };
            lr.positionCount = c.Length;
            lr.SetPositions(c);
        }

        private void Limpiar()
        {
            lineas.Clear();
            if (contenedor != null) Destroy(contenedor.gameObject);
            contenedor = null;
        }
    }
}
