using System;
using UnityEngine;
using UnityEngine.UI;

public class BuildPage : MonoBehaviour
{
    public Transform pageParent;

    public Transform builScrollParent;
    public SScroll buildScroll;
    private void Start()
    {
        buildScroll = builScrollParent.GetComponent<SScroll>();
        buildScroll.SStart();

        ct.LoadAfterInconsFinishLoading = () =>
        {
            foreach (var sp in ct.structIcons.Values)
            {
                buildScroll.Add(g =>
                {
                    g.GetComponent<Image>().sprite = sp;
                });
            }
        };
    }
}
