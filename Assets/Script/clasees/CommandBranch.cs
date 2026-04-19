using System.Collections.Generic;

public class CommandBranch
{
    public string name;
    public CMDMeth execute;
    public List<CommandBranch> branches = new List<CommandBranch>();
    public CMDSuggestion suggestion;

    public CommandBranch AddBranch(string name)
    {
        var b = new CommandBranch(name);
        branches.Add(b);
        return b;
    }

    /// <summary>
    /// Add the execute method of this command branch
    /// </summary>
    /// <param name="execute"></param>
    /// <returns></returns>
    public CommandBranch Execute(CMDMeth execute)
    {
        this.execute = execute;
        return this;
    }

    public delegate bool CMDMeth(Arguments args);

    public delegate List<string> CMDSuggestion();

    public CommandBranch(string name)
    {
        this.name = name;
    }


    public class Arguments
    {

    }
}