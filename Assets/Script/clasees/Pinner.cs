
using System.Collections.Generic;

/// <summary>
/// contain a condition that be containing from keys
/// </summary>
public class Pinner
{
    public bool pinned;
    public List<string> pinners = new List<string>();

    /// <summary>
    ///
    /// </summary>
    /// <param name="pinner"></param>
    /// <param name="add">add pinner or remove </param>
    public void Pin(string pinner, bool add)
    {
        if(add)
            AddPin(pinner);
        else
            RemovePin(pinner);
    }

    public void AddPin(string pinner)
    {
        if(!pinners.Contains(pinner))
            pinners.Add(pinner);
    }

    public void RemovePin(string pinner)
    {
        if (pinners.Contains(pinner))
        {
            pinners.Remove(pinner);
        }
    }

    /// <summary>
    /// if all pinners is removed
    /// </summary>
    public bool loosing => pinners.Count == 0;

    public bool Loose() => loosing;
}