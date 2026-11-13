using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Datos de un objeto del escenario: tipo de sprite, tamaño en el mundo y huella de colisión
    /// (en coordenadas del mundo relativas a su base). Lo usa la herramienta
    /// "Reutilizar objetos del Piso 3" para cambiar el sprite sin mover nada.
    /// </summary>
    public class ObjetoEscenario : MonoBehaviour
    {
        public string tipo;
        [Tooltip("Objeto propio del escenario 1 (no se reemplaza por uno del Piso 3).")]
        public bool propioEscenario1;
        public Vector2 tamanoMundo;
        public Vector2 huellaCentro;
        public Vector2 huellaTamano;
    }
}
