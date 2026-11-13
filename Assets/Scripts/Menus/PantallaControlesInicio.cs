using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[DefaultExecutionOrder(500), RequireComponent(typeof(UIDocument))]
public class PantallaControlesInicio : MonoBehaviour
{
    public static bool IsShowing { get; private set; }
    public static bool BloqueaJuego => IsShowing || transicion;
    private static bool transicion;
    public GameObject jugador;
    public StyleSheet estilos;
    public StyleSheet estilosMenu;
    public Sprite iconoTarjeta;
    public PuertaNivel panelSalida;
    [Min(.1f)] public float duracionFundido = .3f;
    private VisualElement raiz, fondo, panel, objetivo;
    private Button continuar;
    private PlayerInput entrada;
    private readonly Dictionary<string, Label> teclas = new Dictionary<string, Label>();
    private Label descripcion, ayudaContinuar, indicacionSalida, textoObjetivo;
    private PlayerInventory inventario;
    private bool cerrando, poseePausa, pausaAnterior, confirmacionLista;
    private float tiempoAnterior, inicio, inicioCierre;
    private string esquema;
    private Texture2D texturaBoton;
    public VisualElement Panel => panel;

    private void OnEnable() { IsShowing = true; transicion = false; }

    private void Start()
    {
        entrada = jugador != null ? jugador.GetComponent<PlayerInput>() : null;
        inventario = jugador != null ? jugador.GetComponent<PlayerInventory>() : null;
        raiz = GetComponent<UIDocument>().rootVisualElement;
        raiz.Clear(); raiz.AddToClassList("inicio-root");
        if (estilosMenu != null) raiz.styleSheets.Add(estilosMenu);
        if (estilos != null) raiz.styleSheets.Add(estilos);
        raiz.pickingMode = PickingMode.Ignore;
        fondo = new VisualElement { name = "FondoControles" }; fondo.AddToClassList("intro-overlay"); raiz.Add(fondo);
        panel = new VisualElement { name = "PanelControles" }; panel.AddToClassList("intro-panel"); fondo.Add(panel);
        var cabecera = new VisualElement(); cabecera.AddToClassList("intro-header"); panel.Add(cabecera);
        var insignia = Texto("L4", "intro-insignia"); cabecera.Add(insignia);
        var titulos = new VisualElement(); titulos.AddToClassList("intro-titulos"); cabecera.Add(titulos);
        titulos.Add(Texto("PROTOCOLO DE INGRESO  /  RECEPCIÓN", "intro-kicker"));
        titulos.Add(Texto("Conoce tus controles", "intro-title"));
        descripcion = Texto("", "intro-subtitle"); panel.Add(descripcion);
        var columnas = new VisualElement(); columnas.AddToClassList("intro-columns"); panel.Add(columnas);
        var combate = new VisualElement(); combate.AddToClassList("intro-column"); combate.AddToClassList("intro-column-left"); columnas.Add(combate);
        var equipo = new VisualElement(); equipo.AddToClassList("intro-column"); columnas.Add(equipo);
        combate.Add(Texto("MOVIMIENTO Y COMBATE", "intro-section"));
        equipo.Add(Texto("EQUIPO Y SUPERVIVENCIA", "intro-section"));
        Fila(combate, "Move", "Moverte"); Fila(combate, "LookMouse", "Apuntar");
        Fila(combate, "Attack", "Atacar / disparar"); Fila(combate, "SwitchWeapon", "Cambiar arma"); Fila(combate, "Reload", "Recargar");
        Fila(equipo, "Ability", "Habilidad especial"); Fila(equipo, "Interact", "Interactuar");
        Fila(equipo, "ConsumirVenda", "Usar venda"); Fila(equipo, "ConsumirEstimulante", "Usar adrenalina");
        Fila(equipo, "Pause", "Pausar");
        var mision = new VisualElement(); mision.AddToClassList("intro-mission"); panel.Add(mision);
        var imagen = new Image { sprite = iconoTarjeta, scaleMode = ScaleMode.ScaleToFit }; imagen.AddToClassList("intro-card-icon"); mision.Add(imagen);
        var copy = new VisualElement(); copy.AddToClassList("intro-mission-copy"); mision.Add(copy);
        copy.Add(Texto("TU PRIMER OBJETIVO", "intro-kicker"));
        copy.Add(Texto("Recoge la Tarjeta de Acceso junto a la puerta de salida.", "intro-objective"));
        indicacionSalida = Texto("", "intro-hint"); copy.Add(indicacionSalida);
        var pie = new VisualElement(); pie.AddToClassList("intro-footer"); panel.Add(pie);
        ayudaContinuar = Texto("", "intro-hint"); pie.Add(ayudaContinuar);
        continuar = new Button(Cerrar) { text = "ENTENDIDO · COMENZAR", name = "EntendidoButton" };
        continuar.AddToClassList("btn-menu"); continuar.AddToClassList("intro-continue");
        texturaBoton = MainMenuController.CrearGradienteBoton();
        continuar.style.backgroundImage = new StyleBackground(texturaBoton); pie.Add(continuar);
        objetivo = new VisualElement { name = "ObjetivoRecepcion" }; objetivo.AddToClassList("recepcion-objetivo");
        objetivo.pickingMode = PickingMode.Ignore;
        textoObjetivo = Texto("", "recepcion-objetivo-texto"); objetivo.Add(textoObjetivo);
        objetivo.style.display = DisplayStyle.None; raiz.Add(objetivo);
        tiempoAnterior = TransicionEntrePisos.EnCurso ? TransicionEntrePisos.EscalaTiempoAnterior : Time.timeScale;
        pausaAnterior = TransicionEntrePisos.EnCurso ? TransicionEntrePisos.BloqueoPausaAnterior : PauseMenuController.PauseBlocked;
        poseePausa = true; Time.timeScale = 0; PauseMenuController.PauseBlocked = true;
        inicio = Time.unscaledTime; RefrescarControles(); continuar.Focus();
    }

    private void Fila(VisualElement padre, string accion, string titulo)
    {
        var fila = new VisualElement(); fila.AddToClassList("control-row"); padre.Add(fila);
        fila.Add(Texto(titulo, "control-name"));
        var tecla = Texto("", "control-key"); tecla.name = "Binding_" + accion; fila.Add(tecla); teclas[accion] = tecla;
    }
    private static Label Texto(string texto, string clase) { var label = new Label(texto) { pickingMode = PickingMode.Ignore }; label.AddToClassList(clase); return label; }

    private void RefrescarControles()
    {
        esquema = entrada != null ? entrada.currentControlScheme : "Keyboard&Mouse";
        bool mando = esquema == "Gamepad";
        foreach (var par in teclas)
        {
            string nombre = par.Key == "LookMouse" && mando ? "AimGamepad" : par.Key;
            var accion = entrada != null ? entrada.actions.FindAction(nombre, false) : null;
            string binding = par.Key == "Pause" ? (mando ? "MENÚ / START" : "ESC")
                : accion != null ? accion.GetBindingDisplayString(group: mando ? "Gamepad" : "Keyboard&Mouse") : "—";
            if (!mando)
            {
                if (par.Key == "Move") binding = "W A S D  /  FLECHAS";
                if (par.Key == "LookMouse") binding = "RATÓN";
                binding = binding.Replace("LMB", "CLIC IZQUIERDO").Replace("Left Button", "CLIC IZQUIERDO").Replace("Space", "ESPACIO");
            }
            par.Value.text = binding.ToUpperInvariant();
        }
        bool milena = jugador != null && jugador.GetComponent<DashAbility>() != null;
        descripcion.text = milena ? "MILENA  /  Usa tu dash para esquivar y abrirte paso." : "JASON  /  Usa tu escudo para resistir los ataques.";
        ayudaContinuar.text = mando ? "Pulsa A / Cruz para comenzar" : "Pulsa ENTER o haz clic en el botón";
        indicacionSalida.text = "Después acércate al panel de la puerta y pulsa " + teclas["Interact"].text + " para entrar.";
    }

    private void Update()
    {
        if (raiz == null) return;
        if (entrada != null && esquema != entrada.currentControlScheme) RefrescarControles();
        if (textoObjetivo != null && !transicion)
        {
            bool tarjeta = inventario != null && inventario.llavesCount > 0;
            bool enPanel = panelSalida != null && panelSalida.JugadorEnRango;
            textoObjetivo.text = tarjeta
                ? enPanel ? teclas["Interact"].text + " · USAR TARJETA  /  Abrir el panel de salida"
                    : "TARJETA OBTENIDA  /  Acércate al panel de la puerta de salida"
                : "RECEPCIÓN  /  Recoge la tarjeta del suelo, junto a la puerta de salida";
        }
        if (!IsShowing) return;
        if (cerrando)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - inicioCierre) / duracionFundido);
            fondo.style.opacity = 1 - t;
            if (t >= 1)
            {
                fondo.style.display = DisplayStyle.None; objetivo.style.display = DisplayStyle.Flex;
                IsShowing = false; RestaurarTiempo();
            }
            return;
        }
        fondo.style.opacity = Mathf.Clamp01((Time.unscaledTime - inicio) / duracionFundido);
        bool pulsado = false, nuevaPulsacion = false;
        if (entrada != null) foreach (var dispositivo in entrada.devices)
        {
            if (dispositivo is Keyboard k) { pulsado |= k.enterKey.isPressed; nuevaPulsacion |= k.enterKey.wasPressedThisFrame; }
            if (dispositivo is Gamepad g) { pulsado |= g.buttonSouth.isPressed || g.startButton.isPressed; nuevaPulsacion |= g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame; }
        }
        if (!pulsado) confirmacionLista = true;
        if (confirmacionLista && nuevaPulsacion) Cerrar();
    }
    public void Cerrar()
    {
        if (!IsShowing || cerrando || Time.unscaledTime - inicio < .4f) return;
        cerrando = true; inicioCierre = Time.unscaledTime; continuar.SetEnabled(false);
    }
    public void MostrarTransicion()
    {
        transicion = true;
        textoObjetivo.text = "ACCESO AUTORIZADO  /  Entrando al laboratorio…";
    }
    private void RestaurarTiempo()
    {
        if (!poseePausa) return;
        poseePausa = false; Time.timeScale = tiempoAnterior; PauseMenuController.PauseBlocked = pausaAnterior;
    }
    private void OnDisable() { IsShowing = false; transicion = false; RestaurarTiempo(); }
    private void OnDestroy() { if (texturaBoton != null) Destroy(texturaBoton); }
}
