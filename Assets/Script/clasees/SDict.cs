
using System.Collections.Generic;
using System.Linq;

public class SDict<Tkey, Tval> where Tval : class, new()
{
    public int valueCount;
    public Dictionary<Tkey, Dictionary<Tkey, Tval>> dict = new();

    /// <summary>
    /// Set val in dict
    /// </summary>
    /// <param name="overwrite">if has already contained the value overwrite them</param>
    public void Set(Tkey key, Tkey tag, Tval value, bool overwrite = true)
    {
        if (!dict.TryGetValue(key, out Dictionary<Tkey, Tval> line))
        {
            line = new Dictionary<Tkey, Tval>();
            line.Add(tag, value);
            dict.Add(key, line);
        }
        else
        {
            if (!line.TryAdd(tag, value))
            {// if contains tag
                if(overwrite)
                    line[tag] = value;
            }
        }

        valueCount++;
    }

    public Tval Get(Tkey key, Tkey tag) => dict.TryGetValue(key, out Dictionary<Tkey, Tval> line) ? line.GetValueOrDefault(tag) : null;

    /// <summary>
    /// if is not present this location create to that a new
    /// </summary>
    /// <param name="key"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    public Tval GetAbs(Tkey key, Tkey tag)
    {
        if (ExistsLocation(key, tag))
        {
            return Get(key);
        }
        else
        {
            Tval v = new();
            Set(key, tag, v);
            return v;
        }
    }

    /// <summary>
    /// get the first val in line
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Tval Get(Tkey key)
    {
        return dict.TryGetValue(key, out Dictionary<Tkey, Tval> line) ? line.First().Value : null;
    }

    public Tval Get(int index)
    {
        return Get(GetIndexKey(index));
    }

    public List<Tval> Gets(Tkey tag)
    {
        var list = new List<Tval>();
        foreach (var line in dict.Values)
        {
            if (line.TryGetValue(tag, out Tval value))
            {
                list.Add(value);
            }
        }
        return list;
    }

    public int GetKeyIndex(Tkey key)
    {
        return dict.Keys.ToList().IndexOf(key);
    }
    public Tkey GetIndexKey(int index)
    {
        return dict.Keys.ToList()[index];
    }

    public bool ExistsLocation(Tkey key, Tkey tag)
    {
        return dict.TryGetValue(key, out Dictionary<Tkey, Tval> line) && line.ContainsKey(tag);
    }
    public bool TryGet(Tkey key, Tkey tag, out Tval value)
    {
        if (ExistsLocation(key, tag))
        {
            value = Get(key);
            return true;
        }

        value = null;
        return false;
    }
}