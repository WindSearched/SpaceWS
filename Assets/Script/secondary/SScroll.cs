using System;
using UnityEngine;
using UnityEngine.UI;

public class SScroll : MonoBehaviour
{
    public bool justStarted;
    public GameObject template;
    public RectTransform rect;

    public Transform contentParent;
    private RectTransform parentRect;
    public bool scrollable;
    private Vector2 templateSize;
    public Vector2 templateOffset;
    private Vector2 size;
    private int linemax;
    public void SStart()
    {
        parentRect = contentParent.GetComponent<RectTransform>();
        scrollable = contentParent;
        rect = GetComponent<RectTransform>();
        size = rect.sizeDelta;

        SetTemplate(template, templateOffset);

        justStarted = true;
    }

    private void Start()
    {
        if(!justStarted)
            SStart();
    }

    public void SetTemplate(GameObject template, Vector2 offset)
    {
        if(!template || !scrollable) return;

        this.template = template;
        templateSize = offset;
        var r = template.GetComponent<RectTransform>();
        templateSize = r.sizeDelta;
        r.pivot = new(0, 1);

        linemax = (int)(size.x / (templateSize.x + offset.x));
    }

    public void Add(SScrollMeth meth, int index)
    {
        var o = contentParent.Find(template.name + index)?.gameObject;
        if(!o) o = Instantiate(template, contentParent);
        o.name = template.name + index;

        meth?.Invoke(o);
        var r = o.GetComponent<RectTransform>();

        int x = index % linemax;
        int y = index / linemax;
        Vector2 i = new Vector2(x, y);
        Vector2 p = new Vector2(x * (templateSize.x + templateOffset.x), -y * (templateSize.y - templateOffset.y)) + templateOffset;
        r.anchoredPosition = p;

        parentRect.sizeDelta = new(parentRect.sizeDelta.x, -p.y);
    }

     public void Add(SScrollMeth meth) => Add(meth, contentParent.childCount);
}
public delegate void SScrollMeth(GameObject go);