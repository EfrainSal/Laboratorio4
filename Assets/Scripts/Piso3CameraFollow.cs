using UnityEngine;
public class Piso3CameraFollow : MonoBehaviour
{
    public Transform target;
    void LateUpdate(){if(target!=null)transform.position=new Vector3(target.position.x,target.position.y,-10);}
}
