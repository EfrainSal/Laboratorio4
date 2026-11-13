using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Laboratorio4
{
    /// <summary>Lectura de teclado compatible con el Input System nuevo y el antiguo.</summary>
    public static class Entrada
    {
        public static Vector2 Direccion()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }

        /// <summary>Espacio, J o clic izquierdo.</summary>
        public static bool Disparo()
        {
#if ENABLE_INPUT_SYSTEM
            bool t = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame);
            bool m = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            return t || m;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        /// <summary>Escudo de Jason: tecla Q o clic derecho.</summary>
        public static bool Escudo()
        {
#if ENABLE_INPUT_SYSTEM
            bool t = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
            bool m = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            return t || m;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }

        public static bool Tecla(char c)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            if (c == 'R') return k.rKey.wasPressedThisFrame;
            if (c == 'C') return k.cKey.wasPressedThisFrame;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (c == 'R') return Input.GetKeyDown(KeyCode.R);
            if (c == 'C') return Input.GetKeyDown(KeyCode.C);
            return false;
#else
            return false;
#endif
        }
    }
}
