using UnityEngine;

public class MouseRaycast
{
    public delegate void CastEv(GameObject casted);

    public static float distance = 100;

    public GameObject casted;
    public GameObject excasted;

    public MouseRaycast()
    {
        ct.log.Write("Raycast", "load a raycast");
    }

    public event CastEv InCast;
    public event CastEv OutCast;

    /// <summary>
    ///     can be update
    /// </summary>
    public void Casting()
    {
        var layerToIgnore = LayerMask.NameToLayer("raynocast"); // 图层名
        var layerMask = ~(1 << layerToIgnore); // 取反，排除这个图层

        var p = ct.mousePosition;
        var ray = Camera.main.ScreenPointToRay(p);

        excasted = casted; //update

        casted = Physics.Raycast(ray, out var hit, distance, layerMask, QueryTriggerInteraction.Ignore)
            ? hit.collider.gameObject
            : null;


        if (casted != excasted)
        {
            OutCast?.Invoke(excasted);
            InCast?.Invoke(casted);
        }
    }
}