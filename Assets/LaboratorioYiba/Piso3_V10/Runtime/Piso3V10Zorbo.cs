using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Zorbo (enemigo, combate cuerpo a cuerpo previsto). Por ahora está estático:
    /// muestra uno de sus 8 fotogramas de orientación y bloquea el paso con su collider.
    /// La IA, el ataque y la vida se añadirán después.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Piso3V10Zorbo : MonoBehaviour
    {
        [Tooltip("Fotogramas Zorbo_01..08 (orientaciones).")]
        public Sprite[] orientaciones = new Sprite[8];

        [Range(0, 7)]
        public int orientacion = 0;

        private void OnValidate() { Aplicar(); }
        private void Awake() { Aplicar(); }

        public void Aplicar()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null || orientaciones == null || orientaciones.Length == 0) return;
            int i = Mathf.Clamp(orientacion, 0, orientaciones.Length - 1);
            if (orientaciones[i] != null) sr.sprite = orientaciones[i];
        }
    }
}
