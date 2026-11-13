using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Laboratorio4.Escenario3
{
    /// <summary>Lee teclado y ratón con el sistema de entrada que tenga el proyecto (nuevo, antiguo o ambos).</summary>
    public static class E3Entrada
    {
        public static Vector2 Movimiento()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (v == Vector2.zero)
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
            }
#endif
            return v;
        }

        /// <summary>Espacio, J o clic izquierdo (mantenido).</summary>
        public static bool Disparando()
        {
            bool r = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            if (k != null && (k.spaceKey.isPressed || k.jKey.isPressed)) r = true;
            if (m != null && m.leftButton.isPressed) r = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J) || Input.GetMouseButton(0)) r = true;
#endif
            return r;
        }

        /// <summary>Q o clic derecho (al pulsar).</summary>
        public static bool Escudo()
        {
            bool r = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            if (k != null && k.qKey.wasPressedThisFrame) r = true;
            if (m != null && m.rightButton.wasPressedThisFrame) r = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1)) r = true;
#endif
            return r;
        }

        public static bool VerColisiones()
        {
            bool r = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) r = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.C)) r = true;
#endif
            return r;
        }

        public static bool Reiniciar()
        {
            bool r = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) r = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R)) r = true;
#endif
            return r;
        }
    }
}
