using System.Collections.Generic;
using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>
    /// Muro de cobertura dibujado como sprite propio. Su orden es el de su base sur: quien está detrás
    /// (más arriba en pantalla) queda tapado por el muro, como en la perspectiva 3/4 del resto del escenario.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class E4Columna : MonoBehaviour
    {
        public static readonly List<E4Columna> Todas = new List<E4Columna>();
        private SpriteRenderer sr;

        private void OnEnable()
        {
            sr = GetComponent<SpriteRenderer>();
            if (!Todas.Contains(this)) Todas.Add(this);
        }

        private void OnDisable() { Todas.Remove(this); }

        public Bounds Limites { get { return sr != null ? sr.bounds : new Bounds(transform.position, Vector3.zero); } }
    }
}
