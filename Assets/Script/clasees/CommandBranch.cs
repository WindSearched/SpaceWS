using System;
using System.Collections.Generic;
using System.Linq;

public class CommandBranch
{
    public string name;
    public Func<CommandArg, MethValues, bool> execute;
    public Func<List<string>> suggestion;
    public List<CommandBranch> branches = new List<CommandBranch>();
    public List<Argument> arguments = new List<Argument>();

    public CommandBranch AddBranch(CommandBranch branch)
    {
        branches.Add(branch);
        return this;
    }

    public CommandBranch AddBranches(params CommandBranch[] branches)
    {
        foreach (var branch in branches)
            AddBranch(branch);
        return this;
    }

    public CommandBranch AddArgument(Argument argument)
    {
        arguments.Add(argument);
        return this;
    }

    public CommandBranch AddArguments(params Argument[] arguments)
    {
        foreach (var argument in arguments)
            AddArgument(argument);
        return this;
    }

    /// <summary>
    /// Add the execute method of this command branch
    /// </summary>
    /// <param name="execute"></param>
    /// <returns></returns>
    public CommandBranch Execute(Func<CommandArg, MethValues, bool> execute)
    {
        this.execute = execute;
        return this;
    }

    public CommandBranch SetSuggestion(Func<List<string>> suggestion)
    {
        this.suggestion = suggestion;
        return this;
    }

    public CommandBranch Parse(List<string> splited, out CommandArg args)
    {
        var s = splited[0];//get the first value
        foreach (var branch in branches.Where(branch => branch.name == s))
        {   // parse if is existed branch named s
            splited.RemoveAt(0);
            return branch.Parse(splited, out args);
        }

        if (splited.Count != arguments.Count)
        {
            args = null;
            return this;
        }

        args = new CommandArg();
        for (int i = 0; i < arguments.Count; i++)
        {
            var arg = arguments[i];
            args.SetArg(arg.argument, splited[i]);
        }
        return this;
    }

    public bool Command(string arg)
    {
        var list = arg.Split(' ').ToList();
        var branch = Parse(list, out var args);
        return branch.execute != null && branch.execute.Invoke(args, ct.methValues);
    }

    public delegate List<string> CMDSuggestion();

    public CommandBranch(string name)
    {
        this.name = name;
    }

    public class Argument
    {
        public string argument;
        public Func<List<string>> suggestion;

        public Argument(string argument)
        {
            this.argument = argument;
        }

        public Argument SetSuggestion(Func<List<string>> suggestion)
        {
            this.suggestion = suggestion;
            return this;
        }
    }
    public class CommandArg
    {
        public Dictionary<string, string> args;

        public string Get(string key) => args.ContainsKey(key) ? args[key] : null;

        public string Get(int index)
        {
            var a = args.Values.ToList();
            return a.Count > index ? a[index] : null;
        }

        public void SetArg(string key, string value, bool overwrite = true)
        {
            if (args.TryAdd(key, value)) return;
            if(overwrite)
                args[key] = value;
        }
    }
}

public class MethValues
{
    public char decider = '@';
    public Dictionary<string, Func<object>> meths = new Dictionary<string, Func<object>>();

    public string LoadString(string arg)
    {
        var o = Load(arg);
        if(o is string s)
        {
            return s;
        }
        return null;
    }

    public int LoadInt(string arg)
    {
        var o = Load(arg);
        if(o is int i)
        {
            return i;
        }
        return 0;
    }
    public float LoadFloat(string arg)
    {
        var o = Load(arg);
        if(o is float f)
        {
            return f;
        }
        return 0;
    }

    public V3 LoadV3(string arg)
    {
        var o = Load(arg);
        if(o is V3 f)
        {
            return f;
        }
        if (o is string s && V3.TryParse(s, out var v))
        {
            return v;
        }
        return V3.zero;
    }

    public object Load(string arg)
    {
        arg = arg.Replace("\u200B", "");
        if (arg.StartsWith(decider.ToString()))
        {
            var a = arg.TrimStart(decider);
            if (meths.ContainsKey(a))
            {
                return meths[a]();
            }
            else
            {
                return null;
            }
        }
        return arg;
    }

    public void Reg(string name, Func<object> func, bool overwrite = true)
    {
        if (meths.ContainsKey(name))
        {
            if (overwrite)
                meths[name] = func;
        }
        else
        {
            meths.Add(name, func);
        }
    }
}