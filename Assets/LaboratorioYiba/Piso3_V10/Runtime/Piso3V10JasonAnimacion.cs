using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Animación fotograma a fotograma de Jason (sin Animator Controller, para que no dependa
    /// de transiciones): quieto en 8 direcciones, caminar derecha / izquierda / arriba y
    /// disparo frente / derecha / izquierda. Los sprites se asignan en el Inspector
    /// (el generador ya los rellena desde Art/Personajes/Jason).
    /// Controles: WASD / flechas para caminar, Espacio, J o clic izquierdo para disparar.
    /// El disparo es solo la animación (aún no hay proyectil ni daño).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Piso3V10JasonAnimacion : MonoBehaviour
    {
        [Header("Quieto: 0 Sur, 1 Sureste, 2 Este, 3 Noreste, 4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste")]
        public Sprite[] quieto = new Sprite[8];

        [Header("Caminar")]
        public Sprite[] caminarDerecha;
        public Sprite[] caminarIzquierda;
        public Sprite[] caminarArriba;
        [Tooltip("La carpeta entregada no trae caminar hacia abajo. Si se deja vacío se usa el sprite quieto 'Sur'.")]
        public Sprite[] caminarAbajo;

        [Header("Disparo")]
        public Sprite[] disparoFrente;
        public Sprite[] disparoDerecha;
        public Sprite[] disparoIzquierda;

        [Header("Velocidad (fotogramas por segundo)")]
        public float fpsCaminar = 10f;
        public float fpsDisparo = 14f;
        [Tooltip("Inmoviliza a Jason mientras dura la animación de disparo.")]
        public bool quietoAlDisparar = true;

        private SpriteRenderer sr;
        private Piso3V10Movimiento movimiento;
        private int direccion8 = 0;          // 0 = Sur
        private bool ultimoLadoDerecho = true;
        private Sprite[] animDisparo;
        private float tiempo;
        private float tiempoDisparo;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            movimiento = GetComponent<Piso3V10Movimiento>();
        }

        private void Update()
        {
            Vector2 dir = movimiento != null ? movimiento.DireccionEntrada : Piso3V10Entrada.Direccion();

            if (animDisparo == null && Piso3V10Entrada.PulsoDisparo())
            {
                animDisparo = ElegirDisparo();
                tiempoDisparo = 0f;
                if (animDisparo == null || animDisparo.Length == 0) animDisparo = null;
            }

            if (animDisparo != null)
            {
                if (movimiento != null) movimiento.bloqueado = quietoAlDisparar;
                tiempoDisparo += Time.deltaTime;
                int f = Mathf.FloorToInt(tiempoDisparo * fpsDisparo);
                if (f >= animDisparo.Length)
                {
                    animDisparo = null;
                    if (movimiento != null) movimiento.bloqueado = false;
                }
                else
                {
                    sr.sprite = animDisparo[f];
                    return;
                }
            }

            if (dir.sqrMagnitude > 0.01f)
            {
                direccion8 = Direccion8(dir);
                if (Mathf.Abs(dir.x) > 0.01f) ultimoLadoDerecho = dir.x > 0f;

                Sprite[] anim;
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    anim = dir.x > 0f ? caminarDerecha : caminarIzquierda;
                else if (dir.y > 0f)
                    anim = caminarArriba;
                else
                    anim = caminarAbajo;

                tiempo += Time.deltaTime;
                if (anim != null && anim.Length > 0)
                    sr.sprite = anim[Mathf.FloorToInt(tiempo * fpsCaminar) % anim.Length];
                else
                    PonerQuieto();
            }
            else
            {
                tiempo = 0f;
                PonerQuieto();
            }
        }

        private void PonerQuieto()
        {
            if (quieto != null && quieto.Length == 8 && quieto[direccion8] != null)
                sr.sprite = quieto[direccion8];
        }

        private Sprite[] ElegirDisparo()
        {
            switch (direccion8)
            {
                case 1: case 2: case 3: return disparoDerecha;
                case 5: case 6: case 7: return disparoIzquierda;
                case 0: return disparoFrente;
                default: return ultimoLadoDerecho ? disparoDerecha : disparoIzquierda; // mirando al norte
            }
        }

        /// <summary>0 Sur, 1 Sureste, 2 Este, 3 Noreste, 4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste.</summary>
        private static int Direccion8(Vector2 d)
        {
            float a = Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg; // Sur = 0, Este = 90
            int i = Mathf.RoundToInt(a / 45f);
            return ((i % 8) + 8) % 8;
        }
    }
}
