using System.Collections.Generic;
using UnityEngine;

/*
Page 和Mode 应的区别：
page 应有进入和进出的逻辑，且为全局，同时间只能进入一个page，切换时调用旧的进出和新的进入逻辑
mode 应为布尔值，且只存有一个逻辑，通过布尔判断逻辑，可为多个同时作用
*/

public class Page
{
    /// <summary>
    /// usually is corrispoding page object, it include all menber of page
    /// </summary>
    public Transform shine;
    public Meth OnInit;
    public Meth OnEnd;
    public string type;

    /// <summary>
    /// default method to active or disactive the object
    /// </summary>
    /// <param name="active"></param>
    public void Active(bool active)
    {
        shine.gameObject.SetActive(active);
    }

    public Page(Transform shine)
    {
        this.shine = shine;
    }

    public Page(Transform shine, Meth onInit, Meth onEnd)
    {
        this.shine = shine;
        OnInit = onInit;
        OnEnd = onEnd;
    }
}

public class PageStm
{
    public Dictionary<string, Page> pages = new();
    public string current;

    public void Register(string key, Page page)
    {
        if (pages.ContainsKey(key))
            ct.log.Write("PageStm", $"Try to register page {key}, but page already exists!");
        else
        {
            pages.Add(key, page);
            ct.log.Write("PageStm", $"Register page {key}");
        }
    }

    /// <summary>
    /// swap the current page
    /// </summary>
    /// <param name="key"></param>
    public void Swap(string key)
    {
        Get(current).OnEnd?.Invoke();
        Get(key).OnInit?.Invoke();
        current = key;
    }
    public Page Get(string key) => pages.ContainsKey(key) ? pages[key] : null;

    /// <summary>
    /// detecte if the current page is key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool IsPage(string key) => current == key;
    public PageStm()
    {
        current = "main";
        Register("main",new (null));

        ct.command.Add("page", (l) => Swap(l.Load()));
    }
}
