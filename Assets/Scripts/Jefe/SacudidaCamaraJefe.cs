using UnityEngine;

// Desplazamiento aditivo posterior al seguimiento. Se retira antes del siguiente LateUpdate.
[DefaultExecutionOrder(20000), DisallowMultipleComponent]
public class SacudidaCamaraJefe : MonoBehaviour
{
    public bool EnCurso => restante > 0;
    public Vector3 DesfaseActual => aplicado;
    private Vector3 aplicado;
    private float restante, duracion, intensidad, frecuencia, fase;
    public void Disparar(float segundos, float amplitud, float ciclos = 24f)
    {
        QuitarDesfase(); duracion = restante = Mathf.Max(.01f, segundos);
        intensidad = Mathf.Max(0, amplitud); frecuencia = Mathf.Max(1, ciclos); fase = 0;
    }
    private void Update() { QuitarDesfase(); }
    private void LateUpdate()
    {
        if (restante <= 0 || Time.timeScale <= 0 || DeathScreenController.GameplayBlocked) return;
        restante = Mathf.Max(0, restante - Time.deltaTime); fase += Time.deltaTime * frecuencia;
        float envolvente = restante / duracion;
        aplicado = new Vector3(Mathf.Sin(fase * 2.31f), Mathf.Sin(fase * 3.73f + 1.2f), 0) * (intensidad * envolvente);
        transform.position += aplicado;
    }
    private void QuitarDesfase() { transform.position -= aplicado; aplicado = Vector3.zero; }
    public void Cancelar() { restante = 0; QuitarDesfase(); }
    private void OnDisable() { Cancelar(); }
}
