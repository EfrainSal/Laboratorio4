using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarLiberacionDisparo
{
    private const string Clave = "Laboratorio4.Disparo.Liberacion";
    private const string Carpeta = "Reports/Disparo/";
    private static int fase, personaje, municion, alSoltar;
    private static double inicio;
    private static float etapa;
    private static GameObject jugador;
    private static PlayerShooting disparo;
    private static Mouse mouse;
    private static Keyboard teclado;
    private static InputAction ataque;
    private static string escena;
    private static readonly List<string> resultados = new List<string>();
    private static readonly List<string> errores = new List<string>();

    static ProbarLiberacionDisparo()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, t, tipo) =>
        {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (tipo == LogType.Error || tipo == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Disparo/Probar pulsacion y liberacion %&F9")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null;
        StageUtility.GoToMainStage();
        Directory.CreateDirectory(Carpeta);
        SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        EditorApplication.EnterPlaymode();
    }
    private static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run-prueba.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking-prueba.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            fase = personaje = 0; jugador = null; disparo = null;
            resultados.Clear(); errores.Clear();
            inicio = EditorApplication.timeSinceStartup;
        }
        if (estado == PlayModeStateChange.ExitingPlayMode) QuitarDispositivos();
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false);
            SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null;
            RankingManager.SuspenderSincronizacionAutomatica = false;
            EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        }
    }
    private static void Exigir(bool valor, string mensaje)
    {
        if (!valor) throw new Exception(mensaje);
        resultados.Add("PASS " + (personaje == 0 ? "Jason: " : "Milena: ") + mensaje);
    }
    private static void Avanzar(int siguiente) { fase = siguiente; etapa = Time.unscaledTime; }
    private static void Boton(bool pulsado)
    {
        Vector2 posicion = Camera.main.WorldToScreenPoint(jugador.transform.position + Vector3.right * 2);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = posicion, buttons = (ushort)(pulsado ? 1 : 0) });
    }
    private static void Actualizar()
    {
        if (!SessionState.GetBool(Clave, false) || !EditorApplication.isPlaying || fase < 0) return;
        float transcurrido = Time.unscaledTime - etapa;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 90) throw new Exception("Timeout fase " + fase);
            if (fase == 0 && EditorApplication.timeSinceStartup - inicio > .6)
            {
                escena = new SerializedObject(Object.FindFirstObjectByType<MainMenuController>()).FindProperty("_gameSceneName").stringValue;
                GameManager.Instance.IniciarNuevaRun(personaje);
                Avanzar(1); SceneManager.LoadScene(escena); return;
            }
            if (fase == 1 && transcurrido > 1 && SceneManager.GetActiveScene().name == escena)
            {
                jugador = GameObject.FindGameObjectWithTag("Player");
                disparo = jugador.GetComponent<PlayerShooting>();
                foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) s.enabled = false;
                foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
                jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
                jugador.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                Camera.main.GetComponent<LaboratorioYiba.Piso3V10.Piso3V10Camara>().enabled = false;
                Camera.main.transform.position = new Vector3(jugador.transform.position.x, jugador.transform.position.y, Camera.main.transform.position.z);
                teclado = InputSystem.AddDevice<Keyboard>("TecladoPruebaDisparo");
                mouse = InputSystem.AddDevice<Mouse>("MousePruebaDisparo");
                var input = jugador.GetComponent<PlayerInput>();
                input.neverAutoSwitchControlSchemes = true;
                input.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
                ataque = input.currentActionMap.FindAction("Attack", true);
                Exigir(ataque.type == InputActionType.Button, "Accion Attack original de tipo Button");
                jugador.GetComponent<WeaponManager>().EquipWeapon(false);
                disparo.RestaurarMunicion(disparo.maxClipSize, 60);
                Boton(false); municion = disparo.currentAmmoInClip;
                Avanzar(2); return;
            }
            if (fase == 2 && transcurrido > .1f)
            {
                Exigir(disparo.currentAmmoInClip == municion, "Sin clic no dispara");
                Boton(true); Avanzar(3); return;
            }
            if (fase == 3 && transcurrido > disparo.fireRate * 2.2f)
            {
                Exigir(disparo.currentAmmoInClip <= municion - 2, "Mantener clic genera disparos a la cadencia configurada");
                alSoltar = disparo.currentAmmoInClip;
                Boton(false); Avanzar(4); return;
            }
            if (fase == 4 && transcurrido > .06f)
            {
                Exigir(!ataque.IsPressed(), "La accion reconoce la liberacion del clic");
                Exigir(disparo.currentAmmoInClip == alSoltar, "Liberar clic corta el disparo desde la siguiente actualizacion de input");
                Avanzar(5); return;
            }
            if (fase == 5 && transcurrido > disparo.fireRate * 2.2f)
            {
                Exigir(disparo.currentAmmoInClip == alSoltar, "Soltar clic detiene todos los disparos siguientes (municion=" + disparo.currentAmmoInClip + ", esperada=" + alSoltar + ")");
                municion = disparo.currentAmmoInClip;
                Boton(true); Avanzar(6); return;
            }
            if (fase == 6 && transcurrido > .06f)
            {
                Exigir(disparo.currentAmmoInClip == municion - 1, "Nueva pulsacion corta dispara una sola bala");
                Boton(false); Avanzar(7); return;
            }
            if (fase == 7 && transcurrido > disparo.fireRate * 2.2f)
            {
                Exigir(disparo.currentAmmoInClip == municion - 1, "Clic corto no deja el arma disparando");
                Boton(true); Avanzar(8); return;
            }
            if (fase == 8 && transcurrido > .08f)
            {
                Object.FindFirstObjectByType<PauseMenuController>().Pause();
                Boton(false); alSoltar = disparo.currentAmmoInClip;
                Avanzar(9); return;
            }
            if (fase == 9 && transcurrido > .25f)
            {
                Exigir(disparo.currentAmmoInClip == alSoltar, "Pausa impide disparar");
                Object.FindFirstObjectByType<PauseMenuController>().Resume();
                Avanzar(10); return;
            }
            if (fase == 10 && transcurrido > disparo.fireRate * 2.2f)
            {
                Exigir(disparo.currentAmmoInClip == alSoltar, "Soltar durante pausa no reanuda disparos al volver");
                Boton(true); Avanzar(11); return;
            }
            if (fase == 11 && transcurrido > .08f)
            {
                jugador.GetComponent<WeaponManager>().EquipWeapon(true);
                Boton(false); alSoltar = disparo.currentAmmoInClip;
                Avanzar(12); return;
            }
            if (fase == 12 && transcurrido > .1f)
            {
                jugador.GetComponent<WeaponManager>().EquipWeapon(false);
                Avanzar(13); return;
            }
            if (fase == 13 && transcurrido > disparo.fireRate * 2.2f)
            {
                Exigir(disparo.currentAmmoInClip == alSoltar, "Volver del machete con clic liberado no dispara");
                Boton(true); Avanzar(14); return;
            }
            if (fase == 14 && transcurrido > .08f)
            {
                jugador.GetComponent<PlayerInput>().DeactivateInput();
                alSoltar = disparo.currentAmmoInClip;
                Boton(false); Avanzar(15); return;
            }
            if (fase == 15 && transcurrido > disparo.fireRate * 2.2f)
            {
                Exigir(disparo.currentAmmoInClip == alSoltar, "Desactivar PlayerInput detiene el arma aunque el ultimo evento fuera pulsar");
                jugador.GetComponent<PlayerInput>().ActivateInput();
                Avanzar(16); return;
            }
            if (fase == 16 && transcurrido > disparo.fireRate * 2.2f)
            {
                Exigir(disparo.currentAmmoInClip == alSoltar, "Reactivar input con boton libre mantiene el arma detenida");
                QuitarDispositivos(); jugador = null; disparo = null;
                if (personaje == 0)
                {
                    personaje = 1; GameManager.Instance.IniciarNuevaRun(personaje);
                    Avanzar(1); SceneManager.LoadScene(escena);
                }
                else Terminar();
            }
        }
        catch (Exception e) { errores.Add(e.Message); Terminar(); }
    }
    private static void QuitarDispositivos()
    {
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
        mouse = null; teclado = null;
    }
    private static void Terminar()
    {
        fase = -1;
        File.WriteAllLines(Carpeta + "playmode.txt", new[] { errores.Count == 0 ? "RESULTADO: PASS" : "RESULTADO: FAIL" }.Concat(resultados).Concat(errores.Select(e => "FAIL " + e)));
        Debug.Log("[DisparoLiberacion] " + (errores.Count == 0 ? "PASS" : "FAIL") + " " + string.Join(" | ", errores));
        EditorApplication.ExitPlaymode();
    }
}
