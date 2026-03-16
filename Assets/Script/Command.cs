using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class Command
{
    public Dictionary<string,CommM> commands = new();
    /// <summary>
    /// the method value methods
    /// </summary>
    public Dictionary<string, ValMeth> valuemethod = new();

    public void Add(string name, CommM meth,bool overwrite =  false)
    {
        if (commands.ContainsKey(name))
        {
            if (overwrite)
                commands[name] = meth;
            else
            {
                Debug.LogWarning("Duplicate command name: " + name);
            }
        }
        else
        {
            commands.Add(name, meth);
        }
    }

    public void AddValueMethod(string name, ValMeth meth, bool overwrite = false)
    {
        if (valuemethod.ContainsKey(name))
        {
            if (overwrite)
                valuemethod[name] = meth;
            else
            {
                Debug.LogWarning("Duplicate method value name: " + name);
            }
        }
        else
        {
            valuemethod.Add(name, meth);
        }
    }

    public CommM Get(string name)
    {
        return commands.GetValueOrDefault(name);
    }
    public void Load(string line)
    {
        Line l = new(line,this);
        var c = Get(l.Load());
        c?.Invoke(l);
    }


    public class Line
    {
        private Command command;
        public List<string> args = new();

        public Line(string line,Command cmd)
        {
            Split(line);
            command = cmd;
        }

        public void Split(string line) => args = line.Split(" ").ToList();
        public string Get(int index) => args[index];
        public bool LoadValMeth(string methName, out object obj)
        {
            bool b = command.valuemethod.TryGetValue(methName, out var meth);
            foreach (var c in command.valuemethod.Keys)
            {
                Debug.Log(c.Length);
            }
            obj = b ? meth?.Invoke() : null;
            return b;
        }

        /// <summary>
        /// get the first arg and remove it at list
        /// </summary>
        public string Load()
        {
            var line = args[0].TrimStart('@')
                .Replace("\u200B", "");

            args.RemoveAt(0);
            if (line.StartsWith("@"))
            {
                string s = line;

                if (!LoadValMeth(s, out object obj))
                {
                    string m =$"The method value {s} is not exists!";
                    ct.log.Write("Command", m);
                    Debug.Log(m);
                    Debug.Log(s.Length);
                    return null;
                }

                if (obj is float f) line = f.ToString();
                else if (obj is int i) line = i.ToString();
                else line = (string)obj;
            }
            return line;
        }

        public float LoadFloat()
        {
            var l = Load();
            if (!l.StartsWith("@")) return float.TryParse(l, out float result) ? result : 0f;
            if (LoadValMeth(l.TrimStart("@"), out object obj)) return (float)obj;
            string m =$"The method value {l} is not exists!";
            ct.log.Write("Command", m);
            Debug.Log(m);
            return (float)obj;

        }

        public int LoadInt()
        {
            var l = Load();
            if (!l.StartsWith("@")) return int.TryParse(l, out int result) ? result : 0;
            if (LoadValMeth(l.TrimStart("@"), out object obj)) return (int)obj;
            string m =$"The method value {l} is not exists!";
            ct.log.Write("Command", m);
            Debug.Log(m);
            return (int)obj;
        }

        /// <returns>remove three arg at list and ret. a V3 value</returns>
        public V3 LoadV3() => new(LoadFloat(),LoadFloat(),LoadFloat());
        public V3I LoadV3I() => new(LoadInt(),LoadInt(),LoadInt());

        public bool TryLoad([CanBeNull] out string arg)
        {
            arg = null;
            if (args.Count <= 0)
                return false;
            else
            {
                arg = Load();
                return true;
            }
        }

        public void RemoveUntil(int index)
        {
            int i = 0;
            while (i < index)
            {
                args.RemoveAt(0);
                i++;
            }
        }
    }
}
public delegate void CommM(Command.Line line);
public delegate object ValMeth();