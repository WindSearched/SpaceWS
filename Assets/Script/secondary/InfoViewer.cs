using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InfoViewer : MonoBehaviour
{
    public List<string> views;
    public List<string> names;
    public List<TextMeshProUGUI> texts;

    public GameObject textTemplate;
    public float rheight = 20;
    public float large;
    public Transform textParent;

    public RectTransform rect;
    void Start()
    {
        textTemplate = Resources.Load<GameObject>("infotext");
        textParent = transform;
        rect = GetComponent<RectTransform>();
        large = rect.sizeDelta.x;
    }

    public void Updating()
    {
        for (int i = 0; i < names.Count; i++)
        {
            string text = names[i] + " : " + texts[i];
            if (i < texts.Count)
            {
                texts[i].text = text;
            }
            else
                AddText(text);
        }
        rect.sizeDelta = new Vector2(large, names.Count * rheight);
    }
    public void AddText(string text)//text to view
    {
        int id = texts.Count;
        var g = Instantiate(textTemplate, textParent);
        g.name = id.ToString();

        var r = g.GetComponent<RectTransform>();
        r.anchoredPosition = new(0, id*rheight);

        var t =  g.GetComponent<TextMeshProUGUI>();
        texts.Add(t);
        t.text = text;
    }

    public void AddView(string name, string view)
    {
        views.Add(name);
        names.Add(name);
    }
    /// <summary>
    /// get views text in obj value properties
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="_names"></param>
    /// <typeparam name="T"></typeparam>
    public void AddViews<T>(T obj, List<string> _names)
    {
        var _views = STool.ReflectionUtil.GetMemberValues(obj, _names);
        if (_names.Count == _views.Count)
        {
            names.AddRange(_names);
            views.AddRange(_views);
        }
        else
        {
            ct.log.Write("InfoViewer", "view value names contain no exist name");
        }
    }

    public void Clear(bool update)
    {
        views.Clear();
        names.Clear();

        if(update)
            Updating();
    }
}
