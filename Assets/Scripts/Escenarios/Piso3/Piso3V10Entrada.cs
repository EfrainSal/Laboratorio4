using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Lectura de teclado compatible con el Input System nuevo y con el Input Manager antiguo.
    /// Evita la InvalidOperationException que apareció cuando el proyecto solo tenía activo
    /// "Input System Package (New)".
    /// </summary>
    public static class Piso3V10Entrada
    {
        public static Vector2 Direccion()
        {
            Vector2 d = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) d.x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) d.x += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) d.y -= 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) d.y += 1f;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            d.x = Input.GetAxisRaw("Horizontal");
            d.y = Input.GetAxisRaw("Vertical");
#endif
            return d.sqrMagnitude > 1f ? d.normalized : d;
        }

        public static bool PulsoDepuracion()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.C);
#else
            return false;
#endif
        }

        /// <summary>Espacio, J o clic izquierdo.</summary>
        public static bool PulsoDisparo()
        {
#if ENABLE_INPUT_SYSTEM
            bool teclado = Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame);
            bool raton = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            return teclado || raton;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        /// <summary>Habilidad: Q, K o clic derecho.</summary>
        public static bool PulsoHabilidad()
        {
#if ENABLE_INPUT_SYSTEM
            bool teclado = Keyboard.current != null &&
                (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.kKey.wasPressedThisFrame);
            bool raton = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            return teclado || raton;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }

        /// <summary>Solo para probar la animación de muerte: M.</summary>
        public static bool PulsoPruebaMuerte()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.M);
#else
            return false;
#endif
        }

        public static bool PulsoReinicio()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
    }
}
