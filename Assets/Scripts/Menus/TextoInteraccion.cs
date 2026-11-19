using UnityEngine.InputSystem;

// Consulta el Gameplay real y la preferencia de entrada ya utilizada por el menú.
public static class TextoInteraccion
{
    public static PlayerInput Entrada => LevelManager.Instance != null && LevelManager.Instance.JugadorActual != null
        ? LevelManager.Instance.JugadorActual.GetComponent<PlayerInput>() : null;
    public static string Boton(PlayerInput entrada = null)
    {
        entrada = entrada != null ? entrada : Entrada;
        bool mando = GestorModoEntrada.UsandoMando;
        var accion = entrada != null ? entrada.actions?.FindAction("Interact", false) : null;
        if (accion != null)
            for (int i = 0; i < accion.bindings.Count; i++)
            {
                var b = accion.bindings[i]; if (b.isComposite || b.isPartOfComposite) continue;
                string ruta = b.effectivePath ?? "";
                if (mando ? !ruta.Contains("Gamepad") && !(b.groups ?? "").Contains("Gamepad") : !ruta.Contains("Keyboard")) continue;
                if (!mando) return accion.GetBindingDisplayString(i).ToUpperInvariant();
                bool playstation = Gamepad.current != null && (Gamepad.current.layout.Contains("Dual") || Gamepad.current.displayName.Contains("PlayStation"));
                if (ruta.EndsWith("/buttonSouth")) return playstation ? "CRUZ" : "A";
                if (ruta.EndsWith("/buttonEast")) return playstation ? "CÍRCULO" : "B";
                if (ruta.EndsWith("/buttonWest")) return playstation ? "CUADRADO" : "X";
                if (ruta.EndsWith("/buttonNorth")) return playstation ? "TRIÁNGULO" : "Y";
                return accion.GetBindingDisplayString(i).ToUpperInvariant();
            }
        return mando ? "A / CRUZ" : "F";
    }
    public static string Aviso(string accion, PlayerInput entrada = null) => "Oprime " + Boton(entrada) + " para " + accion;
    public static string Formatear(string texto, PlayerInput entrada = null)
    {
        string boton = Boton(entrada);
        return (texto ?? "").Replace("{Interact}", boton).Replace("Oprime F ", "Oprime " + boton + " ")
            .Replace("pulsa F ", "pulsa " + boton + " ").Replace("presiona F ", "presiona " + boton + " ").Replace("[F]", "[" + boton + "]");
    }
}
