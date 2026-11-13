using UnityEngine;

namespace Laboratorio4.Recepcion
{
    [DefaultExecutionOrder(100), RequireComponent(typeof(Collider2D))]
    public class TarjetaSalidaRecepcion : MonoBehaviour
    {
        private bool recogida;
        public bool Recogida => recogida;

        private void Start()
        {
            // LevelManager ya restauró el inventario: no duplicar una tarjeta guardada.
            var inventario = FindFirstObjectByType<PlayerInventory>();
            if (inventario != null && inventario.llavesCount > 0) Ocultar();
        }
        private void OnTriggerEnter2D(Collider2D otro) { Recoger(otro); }
        private void OnTriggerStay2D(Collider2D otro) { Recoger(otro); }
        private void Recoger(Collider2D otro)
        {
            if (recogida || DeathScreenController.GameplayBlocked) return;
            var inventario = otro.GetComponentInParent<PlayerInventory>();
            var salud = inventario != null ? inventario.GetComponent<PlayerHealth>() : null;
            if (inventario == null || salud == null || salud.IsDead) return;
            recogida = true;
            inventario.AgregarObjeto(ItemPickup.PickupType.Llave);
            if (LevelManager.Instance != null) LevelManager.Instance.RecogerLlave();
            Ocultar();
        }
        private void Ocultar()
        {
            recogida = true;
            gameObject.SetActive(false);
        }
    }
}
