using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Puerta independiente (sprite propio, no pintada en el fondo).
    /// Mientras está cerrada, sus colliders de bloqueo impiden entrar a la zona de la puerta.
    /// La apertura, animación, sonido o cambio de piso NO están implementados todavía:
    /// Abrir() solo desactiva el bloqueo para que otra mecánica pueda usarlo.
    /// </summary>
    public class Piso3V10Puerta : MonoBehaviour
    {
        public enum TipoPuerta { EntradaDesdePiso2, SalidaAPiso4 }

        public TipoPuerta tipo = TipoPuerta.EntradaDesdePiso2;
        public bool cerrada = true;
        [Tooltip("Colliders que bloquean el acceso (hijo 'BloqueoAcceso').")]
        public Collider2D[] bloqueos;

        private void Awake() { Aplicar(); }
        private void OnValidate() { Aplicar(); }

        public void Abrir() { cerrada = false; Aplicar(); }
        public void Cerrar() { cerrada = true; Aplicar(); }
        public void Alternar() { cerrada = !cerrada; Aplicar(); }

        private void Aplicar()
        {
            if (bloqueos == null) return;
            foreach (Collider2D c in bloqueos)
            {
                if (c != null) c.enabled = cerrada;
            }
        }
    }
}
