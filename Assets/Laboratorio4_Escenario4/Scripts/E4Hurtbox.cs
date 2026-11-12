using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>Zona (trigger) donde los proyectiles dañan al personaje. Va en un hijo, a la altura de los pies.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class E4Hurtbox : MonoBehaviour
    {
        public E4Vida vida;
    }
}
