using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Depository
{
    public Dictionary<string, int> cells;

    public void Deposite(string itemType, int quantity)
    {
        if (!cells.ContainsKey(itemType))
            cells.Add(itemType, 0);
        cells[itemType] += quantity;
    }

    public bool TakeOut(string type, int quantity)
    {
        bool b = QuantityExists(type, quantity);
        if (b)
            cells[type] -= quantity;
        return b;
    }

    public bool QuantityExists(string type, int quantity) => cells.ContainsKey(type) && cells[type] >= quantity;

    public Depository()
    {
        cells = new();
    }
}
