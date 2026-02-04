using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public class Command
{
    public Dictionary<string,CommM> commands = new();

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

    public CommM Get(string name)
    {
        return commands.GetValueOrDefault(name);
    }
    public void Load(string line)
    {
        Line l = new(line);
        var c = Get(l.Load());
        c?.Invoke(l);
    }


    public class Line
    {
        public List<string> args = new();

        public Line(string line) => Split(line);

        public void Split(string line) => args = line.Split(" ").ToList();
        public string Get(int index) => args[index];
        /// <summary>
        /// get the first arg and remove it at list
        /// </summary>
        public string Load()
        {
            var line = args[0];
            args.RemoveAt(0);
            return line;
        }

        public float LoadFloat()
        {
            var l = Load();
            if (float.TryParse(l, out float result))
            {
                return result;
            }
            return 0f;
        }

        public int LoadInt()
        {
            var l = Load();
            if (int.TryParse(l, out int result))
            {
                return result;
            }
            return 0;
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