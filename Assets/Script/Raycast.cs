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
        var p = ct.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(p);

        excasted = casted;//update
        if (Physics.Raycast(ray, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            casted = hit.collider.gameObject;
            if (casted != excasted)
            {
                if (casted)
                    InCast?.Invoke(casted);
                if(excasted)
                    OutCast?.Invoke(excasted);
            }
        }
    }

    public MouseRaycast()
    {
        ct.log.Write("Raycast","load a raycast");
    }
    
    public delegate void CastEv(GameObject casted);
}
