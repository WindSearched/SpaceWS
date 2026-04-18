using System;
using System.Collections.Generic;
using UnityEngine;

public class SLibrary : MonoBehaviour
{
    public Dictionary<string, object> library = new();

    public void Write(string name, object value, bool overwrite = true)
    {
        if (overwrite)
        {
            if(!library.ContainsKey(name))
                library.Add(name, null);
        }
        else
        {
            if(library.ContainsKey(name))
                return;
        }
        library[name] = value;
    }

    public T ReadClass<T>(string name)
    where T : class, new()
    {
        if(library.ContainsKey(name))
            return (T)library[name];
        return new T();
    }
    public T ReadValue<T>(string name, T defaultValue = default)
    {
        if (library.TryGetValue(name, out var value) && value is T t)
            return t;

        return defaultValue;
    }

    public void Clear() => library.Clear();

    public void Remove(string name)
    {
        if (library.ContainsKey(name))
            library.Remove(name);
    }
}
