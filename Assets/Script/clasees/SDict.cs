
using System.Collections.Generic;
using System.Linq;

public class SDict<Tkey, Tval> where Tval : class
{
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
    }

    public Tval Get(Tkey key, Tkey tag) => dict.TryGetValue(key, out Dictionary<Tkey, Tval> line) ? line.GetValueOrDefault(tag) : null;

    /// <summary>
    /// get the first val in line
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Tval Get(Tkey key)
    {
        return dict.TryGetValue(key, out Dictionary<Tkey, Tval> line) ? line.First().Value : null;
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
}