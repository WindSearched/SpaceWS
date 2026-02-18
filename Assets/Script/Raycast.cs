using UnityEngine;
using UnityEngine.Rendering;

public class MouseRaycast
{
    public static float distance = 100;
    
    public GameObject casted;
    public GameObject excasted;
    public event CastEv InCast;
    public event CastEv OutCast;

    /// <summary>
    /// can be update
    /// </summary>
    public void Casting()
    {
        int layerToIgnore = LayerMask.NameToLayer("raynocast"); // 图层名
        int layerMask = ~(1 << layerToIgnore); // 取反，排除这个图层

        var p = ct.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(p);

        excasted = casted;//update

        casted = Physics.Raycast(ray, out RaycastHit hit, distance, layerMask, QueryTriggerInteraction.Ignore)
            ? hit.collider.gameObject : null;


        if (casted != excasted)
        {
            if (casted)
                InCast?.Invoke(casted);
            if(excasted)
                OutCast?.Invoke(excasted);
        }
    }

    public MouseRaycast()
    {
        ct.log.Write("Raycast","load a raycast");
    }
    
    public delegate void CastEv(GameObject casted);
}
