using System;
using System.Collections.Generic;
using UnityEngine;

public class ViewPage : MonoBehaviour
{
    public SMTDict<ViewElementBase> elementbases = new();
    public List<ViewElement> elements = new();
    /// <summary>
    /// index of obj that be removed in next refresh
    /// </summary>
    public List<int> removed;

    private GameObject elTemp;


    private void Start()
    {
        ct.viewPage = this;
        elTemp = Resources.Load<GameObject>("ui/ViewElement");

    }

    public void Add(ViewElementBase b, bool singleupdate = true)
    {
        var g = Instantiate(elTemp, transform);
        var e = g.GetComponent<ViewElement>();
        e.SetBase(b, singleupdate);
    }

    public void Refresh()
    {
        foreach (var id in removed)
        {
            Remove(id);
        }

        foreach (var e in elements)
        {
            e.Refresh();
        }
    }

    public void Remove(int index, bool singlerefresh = true)
    {
        if (singlerefresh)
        {
            var e = elements[index];
            e.gameObject.SetActive(false);
            e.transform.SetAsLastSibling();
            e.updatestop = true;
            elements.Remove(e);
        }
        else
        {
            removed.Add(index);
        }
    }

    public void Clear(bool refresh = true)
    {
        Action<int> act = refresh ?
            i => Remove(i, elements[i]) :
            i => removed.Add(i);

        for (int i = 0; i < elements.Count; i++)
        {
            act.Invoke(i);
        }
    }
}

public struct ViewElementBase
{
    public Vector2 size;
    public Vector2 position;
    public Vector2 pivot;

    public string text;
    public Sprite sprite;

    public Action<ViewElement, GameObject> enterInput;
    public Action<ViewElement, GameObject> exitInput;
    public Action<ViewElement, GameObject> downInput;
    public Action<ViewElement, GameObject> upInput;

    public Action<ViewElement> update;
}