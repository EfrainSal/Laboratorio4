using UnityEngine;

namespace Laboratorio4.Escenario4
{
    public enum E4Bando { Jugadores, Jefe }

    /// <summary>Direcciones: 0 Sur, 1 Sureste, 2 Este, 3 Noreste, 4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste.</summary>
    public static class E4Util
    {
        public static int Direccion(Vector2 v)
        {
            if (v.sqrMagnitude < 1e-6f) return 0;
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            int d = Mathf.RoundToInt((ang + 90f) / 45f);
            return ((d % 8) + 8) % 8;
        }

        public static Vector2 Vector(int dir)
        {
            float a = (dir * 45f - 90f) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        public static bool MiraAlNorte(int dir) { return dir >= 3 && dir <= 5; }

        /// <summary>Crea un proyectil. La lógica viaja a la altura del suelo (pies) y el dibujo a la altura del arma.</summary>
        public static E4Proyectil CrearProyectil(string nombre, Vector2 origen, Vector2 dir, E4Bando bando, int danio,
            float velocidad, float radio, float vida, Sprite sprite, Sprite impacto, float altura, Transform ignorar, bool rotar)
        {
            GameObject go = new GameObject(nombre);
            go.transform.position = new Vector3(origen.x, origen.y, 0f);
            E4Proyectil p = go.AddComponent<E4Proyectil>();
            p.bando = bando;
            p.danio = danio;
            p.velocidad = velocidad;
            p.radio = radio;
            p.vidaMax = vida;
            p.dir = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.down;
            p.impacto = impacto;
            p.altura = altura;
            p.ignorar = ignorar;

            GameObject vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = new Vector3(0f, altura, 0f);
            if (rotar) vis.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.dir.y, p.dir.x) * Mathf.Rad2Deg);
            SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            E4OrdenY o = vis.AddComponent<E4OrdenY>();
            o.referencia = go.transform;
            o.desfase = 3;
            o.Aplicar();
            p.visual = vis.transform;
            return p;
        }
    }
}
