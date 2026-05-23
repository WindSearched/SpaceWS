using System;
using System.Collections.Generic;
using System.Linq;

public class CommandBranch
{
    public string name;
    public Func<CommandArg, bool> execute;
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
    public CommandBranch Execute(Func<CommandArg, bool> execute)
    {
        this.execute = execute;
        return this;
    }

    public CommandBranch SetSuggestion(Func<List<string>> suggestion)
    {
        this.suggestion = suggestion;
        return this;
    }

    public CommandBranch Parse(List<string> splited, out List<string> args)
    {
        var s = splited[0];//get the first value
        foreach (var branch in branches.Where(branch => branch.name == s))
        {   // parse if is existed branch named s
            splited.RemoveAt(0);
            return branch.Parse(splited, out args);
        }

        args = splited;
        return this;
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
        public Dictionary<string, object> args;

        public object Get(string key) => args.ContainsKey(key) ? args[key] : null;

        public object Get(int index)
        {
            var a = args.Values.ToList();
            return a.Count > index ? a[index] : null;
        }

        public void SetArg(string key, object value, bool overwrite = true)
        {
            if (!args.TryAdd(key, value))
            {
                if(overwrite)
                    args[key] = value;
            }
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


    public object Load(string arg)
    {
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
}