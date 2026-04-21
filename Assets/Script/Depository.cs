using System;
using System.Collections.Generic;
using MemoryPack;
using UnityEngine;
using XLua;

// [Serializable][LuaCallCSharp][MemoryPackable]
// public partial class Depository
// {
//     public Dictionary<SMType, int> cells;
//     public int max;
//
//     public bool Deposite(SMType itemType, int quantity)
//     {
//         if (!cells.ContainsKey(itemType))
//             cells.Add(itemType, 0);
//         var sum = cells[itemType] + quantity;
//         if(sum > max)
//             return false;
//         cells[itemType] = sum;
//         return true;
//     }
//
//     public bool TakeOut(SMType type, int quantity)
//     {
//         bool b = QuantityExists(type, quantity);
//         if (b)
//             cells[type] -= quantity;
//         return b;
//     }
//
//     public bool QuantityExists(SMType type, int quantity) => cells.ContainsKey(type) && cells[type] >= quantity;
//
//     public Depository()
//     {
//         cells = new();
//     }
// }
