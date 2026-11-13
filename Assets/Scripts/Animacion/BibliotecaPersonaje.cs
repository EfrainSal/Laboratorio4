using System;
using UnityEngine;

namespace Laboratorio4.Animacion
{
    [Serializable] public class SpritesDireccion { public Sprite[] frames = Array.Empty<Sprite>(); }
    [Serializable] public class ClipPersonaje
    {
        public string nombre;
        public float fps = 10f;
        public bool bucle;
        public SpritesDireccion[] direcciones = new SpritesDireccion[8];
    }

    [CreateAssetMenu(menuName = "Laboratorio 4/Animaciones de personaje")]
    public class BibliotecaPersonaje : ScriptableObject
    {
        public string personaje;
        public ClipPersonaje[] animaciones = Array.Empty<ClipPersonaje>();
    }
}
