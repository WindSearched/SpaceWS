using System.Collections.Generic;
using UnityEngine;

/*
Page 和Mode 应的区别：
page 应有进入和进出的逻辑，且为全局，同时间只能进入一个page，切换时调用旧的进出和新的进入逻辑
mode 应为布尔值，且只存有一个逻辑，通过布尔判断逻辑，可为多个同时作用
*/

public class Page
{
    public Storer storer;
    public PageMeth OnInit;
    public PageMeth OnEnd;
    public string type;

    public Page(PageMeth onInit, PageMeth onEnd, Storer storer = null)
    {
        this.storer = storer ?? new Storer();
        OnInit = onInit;
        OnEnd = onEnd;
    }

    public Page()
    {
        storer = new Storer();
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
    public bool Swap(string key)
    {
        var cur = Get(current);
        if(cur == null) return false;

        cur.OnEnd?.Invoke(cur);

        var n = Get(key);
        n.OnInit?.Invoke(n);

        current = key;
        return true;
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
        Register("main",new ());

        ct.CommandBranch.AddBranch(
            new CommandBranch("page")
                .AddArgument(new("name"))
                .Execute((a, l) =>
                {
                    var v = a.Get(0);
                    return Swap(v);
                }));
    }
}

public class Storer
{
    public Dictionary<string, object> store = new();

    public bool Add(string name, object value) =>  store.TryAdd(name, value);
    public object Get(string name) => store[name];
    public object Take(string name)
    {
        var o = Get(name);
        store.Remove(name);
        return o;
    }
    public void Clear() => store.Clear();
}
public delegate void PageMeth(Page page);