
using System.Collections.Generic;
using System.Linq;

public class SDict<TKey, TVal> where TVal : new()
{
    public int valueCount;

    public Dictionary<TKey, Dictionary<TKey, TVal>> dict = new();

    /// <summary>
    /// Set val in dict
    /// </summary>
    /// <param name="overwrite">
    /// if has already contained the value overwrite them
    /// </param>
    public void Set(
        TKey key,
        TKey tag,
        TVal value,
        bool overwrite = true)
    {
        if (!dict.TryGetValue(key, out var line))
        {
            line = new Dictionary<TKey, TVal>();
            line.Add(tag, value);
            dict.Add(key, line);
        }
        else
        {
            if (!line.TryAdd(tag, value))
            {
                // already contains tag
                if (overwrite)
                    line[tag] = value;
            }
        }

        valueCount++;
    }

    public TVal Get(TKey key, TKey tag)
    {
        if (dict.TryGetValue(key, out var line) &&
            line.TryGetValue(tag, out var value))
        {
            return value;
        }

        return default;
    }

    /// <summary>
    /// if not present create a new value
    /// </summary>
    public TVal GetAbs(TKey key, TKey tag)
    {
        if (ExistsLocation(key, tag))
        {
            return Get(key, tag);
        }
        else
        {
            TVal v = new();
            Set(key, tag, v);
            return v;
        }
    }

    /// <summary>
    /// get the first val in line
    /// </summary>
    public TVal Get(TKey key)
    {
        if (dict.TryGetValue(key, out var line) &&
            line.Count > 0)
        {
            return line.First().Value;
        }

        return default;
    }

    public TVal Get(int index)
    {
        return Get(GetIndexKey(index));
    }

    public List<TVal> Gets(TKey tag)
    {
        var list = new List<TVal>();

        foreach (var line in dict.Values)
        {
            if (line.TryGetValue(tag, out var value))
            {
                list.Add(value);
            }
        }

        return list;
    }

    public int GetKeyIndex(TKey key)
    {
        return dict.Keys.ToList().IndexOf(key);
    }

    public TKey GetIndexKey(int index)
    {
        return dict.Keys.ToList()[index];
    }

    public bool ExistsLocation(TKey key, TKey tag)
    {
        return dict.TryGetValue(key, out var line)
               && line.ContainsKey(tag);
    }

    public bool TryGet(
        TKey key,
        TKey tag,
        out TVal value)
    {
        if (dict.TryGetValue(key, out var line) &&
            line.TryGetValue(tag, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}

public class SMTDict<TVal> : SDict<string, TVal> where TVal : new()
{
    public TVal Get(SMType smt)
    {
        return Get(smt.type, smt.mod);
    }

    public TVal GetAbs(SMType smt) => GetAbs(smt.type, smt.mod);
    public bool TryGet(SMType smt, out TVal value) => TryGet(smt.type, smt.mod, out value);

    public SMType GetFirstKey(string key)
    {
        if (dict.TryGetValue(key, out var line))
        {
            return new(key, line.Keys.First());
        }
        return new SMType(key, null);
    }

    public void Set(SMType smt, TVal value, bool overwrite = true) => Set(smt.type, smt.mod, value, overwrite);
}