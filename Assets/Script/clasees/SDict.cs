
using System;
using System.Collections.Generic;
using System.Linq;

public class SDict<TKey, TVal>
{
    public int valueCount;

    public Dictionary<TKey, Dictionary<TKey, TVal>> dict = new();

    /// <summary>
    /// factory used when auto create value
    /// </summary>
    public Func<TVal> ValueFactory { get; set; }

    public SDict(Func<TVal> valueFactory = null)
    {
        ValueFactory = valueFactory;
    }

    /// <summary>
    /// Set val in dict
    /// </summary>
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
    /// if not present create one automatically
    /// </summary>
    public TVal GetAbs(TKey key, TKey tag, Func<TVal> createFunc)
    {
        Set(key, tag, createFunc.Invoke(), false);
        return Get(key, tag);
        // if (ExistsLocation(key, tag))
        // {
        //     return Get(key, tag);
        // }
        //
        // TVal v;
        //
        // if (ValueFactory != null)
        // {
        //     v = ValueFactory();
        // }
        // else
        // {
        //     v = default;
        // }
        //
        // Set(key, tag, v);
        //
        // return v;
    }

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

public class SMTDict<TVal> : SDict<string, TVal>
{
    public TVal Get(SMType smt)
    {
        return Get(smt.type, smt.mod);
    }

    public TVal GetAbs(SMType smt, Func<TVal> createFunc) => GetAbs(smt.type, smt.mod, createFunc);
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