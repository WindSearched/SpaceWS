using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemoryPack;
using UnityEngine;
using XLua;

[Serializable][MemoryPackable][LuaCallCSharp]
public partial class StructState : State
{
	public SMType type;
	public Loc relativeLocation;
	public Loc absoluteLocation;
	public int structIndex;

	public SMType material;
	public float temperature;
	public float mass;

	public int choosedRecipeIndex = 0;
	public bool producing;

	public bool buildable;
	public Mixture mixture;
	public StuffList stuffList;
	public Depositor depositor;
	public SMType _setMaterial
	{
		set
		{
			material = value;
			var md = ct.materials.Get(value.type, value.mod);
			StructData sd;
			if (!ct.structsInfo.TryGet(type, out var val))
			{
				string s = $"the struct data of '{type}' is not exists";
				ct.log.Write("StructState._setMaterial",s);
				sd = new();
			}
			else
			{
				sd = val.data;
			}

			mass = sd.volume * md.density;
		}
	}

	public bool isMixture_ => mixture == null;

	public StructState()
	{
		var data = ct.structsInfo.Get(type).data;
		if (data.isFactory_)
		{
			stuffList = new();
			stuffList.containItems.UnlockAdapt(data.factory.recipes[choosedRecipeIndex].materials);
		}
		else if(data.)
		{

		}
	}

	[Serializable][MemoryPackable][LuaCallCSharp]
	public partial class Mixture
	{
		public List<(SMType type, float proportion)> mixture = new();
		private float sum = 0;

		public void Add(SMType type, float proportion)
		{
			mixture.Add((type, proportion));
			sum +=  proportion;
		}

		/// <summary>
		/// calculate proportion with their sum as 1
		/// </summary>
		/// <param name="prop"></param>
		/// <returns></returns>
		public float GetProportion(float prop) => prop / sum;

		public float getSum_ => sum;
		public override string ToString()
		{
			StringWriter sw = new();
			sw.Write("{");
			foreach (var m in mixture)
			{
				sw.Write(m.type);
				sw.Write(",");
				sw.Write(m.proportion);
				sw.Write(";");
			}
			sw.Write("}");
			return sw.ToString();
		}
	}
	[Serializable][MemoryPackable][LuaCallCSharp]
	public partial class StuffList
	{
		public Depositor containItems = new();
		public Dictionary<SMType, List<int>> containStructs = new();// the int value is index of struct

		public bool AddItem(SMType type, int quantity) => containItems.Deposites(type, quantity);

		public bool AddItem(SMType type, int quantity, out int remain)
		{
			containItems.Deposites(type, quantity, out remain);
			return remain == 0;
		}

		public void AddItems(List<Amount> items)
		{
			foreach (var i in items)
			{
				AddItem(i.type, i.quantity);
			}
		}

		public bool RemoveItem(SMType type, int quantity) => containItems.TakeOut(type, quantity);
		public bool ContainItems(SMType type, int quantity) => containItems.Contains(type, quantity);

		public bool ContainItem(SMType type) => containItems.Contains(type);

		public void AddStrut(SMType type, int sid)
		{
			if (containStructs.ContainsKey(type))
			{
				containStructs[type].Add(sid);
			}
			else
			{
				containStructs.Add(type, new(){sid});
			}
		}
		public bool ContainStruct(SMType type)
		{
			return containStructs.ContainsKey(type) && containStructs[type].Count > 0;
		}

		public bool ContainStructs(SMType type, int quantity)
		{
			return containStructs.ContainsKey(type) && containStructs[type].Count >= quantity;
		}

		public bool RemoveAStruct(SMType type)
		{
			if (!ContainStruct(type)) return false;
			containStructs[type].RemoveAt(0);
			return true;
		}

		public bool RemoveStructs(SMType type, int quantity)
		{
			if (ContainStructs(type, quantity))
			{
				containStructs[type].RemoveRange(0, quantity);
				return true;
			}

			return false;
		}


		public bool Contain(StructData.Factory.Recipe recipe)
		{
			if (recipe.materials != null && recipe.materials.Any(mt => !ContainItems(mt.type, mt.quantity)))
			{
				return  false;
			}
			if (recipe.structMaterials != null && recipe.structMaterials.Any(m => !ContainStructs(m.type, m.quantity)))
			{
				return false;
			}

			return true;
		}

		public bool TryRemove(StructData.Factory.Recipe recipe)
		{
			bool mn = recipe.materials == null;
			bool smn = recipe.structMaterials == null;

			if (recipe == null || (mn && smn) || !Contain(recipe)) return false;

			if(!mn)
				foreach (var m in recipe.materials)
				{
					RemoveItem(m.type, m.quantity);
				}
			if(!smn)
				foreach (var m in recipe.structMaterials)
				{
					RemoveStructs(m.type, m.quantity);
				}
			return true;
		}

		public override string ToString()
		{
			StringWriter sw = new();
			sw.WriteLine("{");

			sw.WriteLine("Items:");
			sw.WriteLine(containItems);
			sw.WriteLine("Structs:");
			foreach (var v in containStructs)
			{
				sw.WriteLine(v.Value.Count + " : " + v.Key);
			}

			sw.WriteLine("}");
			return sw.ToString();
		}
	}
	[Serializable][MemoryPackable][LuaCallCSharp]
	public partial class Depositor
	{
		public Dictionary<SMType, int> cells = new();
		public List<SMType> unlocks = new();
		public bool unlockAll;
		public int max;

		public bool Deposites(SMType type, int quantity)
		{
			if (!unlockAll && !unlocks.Contains(type)) return false;
			if (!Contains(type))
			{
				cells.Add(type, 0);
			}
			var sum =  cells[type] + quantity;
			if (sum > max) return false;
			cells[type] = sum;
			return true;
		}

		public void Deposites(SMType type, int quantity, out int remain)
		{
			remain = quantity;
			if (!unlockAll && !unlocks.Contains(type)) return;
			if (!Contains(type))
			{
				cells.Add(type, 0);
			}
			int sum =  cells[type] + quantity;
			if (sum > max)
			{
				remain = sum - max;
				cells[type] = max;
			}
			else
			{
				remain = 0;
				cells[type] = sum;
			}
		}

		public bool TakeOut(SMType type, int quantity)
		{
			if(!unlockAll && !unlocks.Contains(type) && !Contains(type, quantity)) return false;
			cells[type] -= quantity;
			return true;
		}

		public bool Contains(SMType type) => cells.ContainsKey(type);

		public bool Contains(SMType type, int quantity) => Contains(type, quantity) && cells[type] >= quantity;

		public bool IsFull(SMType type) => Contains(type) && cells[type] >= max;

		public void UnlockAdapt(List<Amount> list)
		{
			unlocks.Clear();
			foreach (var m in list)
			{
				unlocks.Add(m.type);
			}
		}
		public override string ToString()
		{
			StringWriter sw = new();
			sw.WriteLine("{");

			foreach (var v in cells)
			{
				sw.WriteLine(v.Value + " : " + v.Key);
			}

			sw.WriteLine("}");
			return sw.ToString();
		}
	}

	public Loc _absLoc
	{
		set
		{
			absoluteLocation = value;
			relativeLocation = new(value.position / ct.setting.chunkUnit, value.rotation);
		}
	}

	public void Locate(GameObject g)
	{
		g.transform.SetPositionAndRotation(absoluteLocation.position.ToVector3(), absoluteLocation.rotation.ToQuaternion());
	}
}

// public class Inventory
// {
//     public List<Grid> invt = new();
//     public event Update WhenInvChange;
//
//     public bool full = false;
//
//     public Inventory() { }
//     public Grid GetGrid(int index)
//     {
//         return invt[index];
//     }
//     /// <summary>
//     /// add never use the index, it can auto look for it
//     /// </summary>
//     /// <param name="item"></param>
//     /// <param name="amt"></param>
//     /// <param name="full">if return 0, the inventory is full</param>
//     public void Add(string item, int amt, out int full)
//     {
//         Grid grid = new(amt, item);
//         Add(grid, out full);
//     }
//     public void Add(int index, int amt, out int full)
//     {
//         Grid g = GetGrid(index);
//         g.Add(amt, out full);
//         Invchange();
//     }
//     /// <summary>
//     ///
//     /// </summary>
//     /// <param name="grid"></param>
//     /// <param name="add">if would remove input -1</param>
//     public void Add(Grid grid, out int full, int add = 1)
//     {
//         Grid g = SearchItemGrid(grid.item);
//         g ??= SearchEmptyGrid(grid.item);
//         if (g == null)
//         {
//             full = grid.amt;
//             this.full = true;
//             return;
//         }
//         else
//         {
//             this.full = false;
//         }
//
//         g.Add(grid.amt * add, out int f2);
//         if (f2 > 0)
//         {
//             grid.amt = f2;
//             Add(grid, out int f3, add);
//             if (f3 > 0)
//             {
//                 full = f3;
//                 g.durab = SetTool(grid.item);
//                 Invchange();
//                 return;
//             }
//         }
//         full = 0;
//         g.durab = SetTool(grid.item);
//         Invchange();
//
//     }
//     public void Switch(int index, Grid from, out Grid to)
//     {
//         to = invt[index];
//         invt[index] = from;
//         Invchange();
//     }
//     /// <summary>
//     /// search the grid,if have not the item grid return null
//     /// </summary>
//     /// <param name="item"></param>
//     /// <param name="searchfullgrid"></param>
//     /// <returns></returns>
//     public Grid SearchItemGrid(string item, bool searchfullgrid = false)
//     {
//         for (int ind = 0; ind < invt.Count; ind++)
//         {
//             if (invt[ind].item == item)
//             {
//                 if (!searchfullgrid && invt[ind].amt < Item.GetData(item).maxAmt)
//                 {
//                     return invt[ind];
//                 }
//                 else if (searchfullgrid)
//                     return invt[ind];
//                 else
//                     continue;
//             }
//         }
//         return null;
//     }
//     public bool HasFreeItemGrid(string item)
//     {
//         foreach (Grid grid in invt)
//         {
//             if (grid.item == item && grid.amt < Item.GetData(item).maxAmt)
//                 return true;
//         }
//         return false;
//     }
//     public Grid SearchEmptyGrid(string insertItem = "n")
//     {
//         for (int ind = 0; ind < invt.Count; ind++)
//         {
//             if (invt[ind].item == "n")
//             {
//                 invt[ind].item = insertItem;
//                 return invt[ind];
//             }
//         }
//         return null;
//     }
//     public Inventory(int grids)
//     {
//         while (grids-- > 0)
//         {
//             invt.Add(new());
//         }
//     }
//     [Serializable]
//     [MessagePackObject]
//     public class Grid
//     {
//         /// <summary>
//         /// amount
//         /// </summary>
//         public int amt;
//         /// <summary>
//         /// type of item
//         /// </summary>
//         [Key(1)] public string item = "n";
//         /// <summary>
//         /// durability
//         /// </summary>
//         [Key(2)] public int durab = -1;
//
//         /// <summary>
//         /// add in the grid, if it is full return surplus, if the grid is empty return full complete
//         /// remove in the grid, if it is <0 return full = -amtT
//         /// </summary>
//         public void Add(int amt, out int full)
//         {
//             if (item == "n")
//             {
//                 full = amt;
//                 return;
//             }
//
//             full = 0;
//             int max = Item.GetData(item).maxAmt;
//             this.amt += amt;
//             switch (this.amt)
//             {
//                 case int i when i > max:
//                     full = this.amt - max;
//                     this.amt = max;
//                     break;
//                 case 0:
//                     item = "n";
//                     durab = -1;
//                     break;
//                 case < 0:
//                     full = -this.amt;
//                     this.amt = max;
//                     item = "n";
//                     durab = -1;
//                     break;
//             }
//         }
//         public void Fray(int frayed)
//         {
//             durab -= frayed;
//             if (durab <= 0)
//             {
//                 amt -= 1;
//                 durab = Item.GetData(item).tool.durability;
//                 if (amt <= 0)
//                     item = "n";
//             }
//
//         }
//
//         public void Insert(Grid grid, out Grid ot)
//         {
//             if (grid.item == item)//add to
//             {
//                 Add(grid.amt, out int full);
//                 if (full > 0)
//                 {
//                     ot = new(full, grid.item, grid.durab);
//                 }
//                 else
//                     ot = new();
//             }
//             else//switch
//             {
//                 ot = this;
//
//                 item = grid.item;
//                 amt = grid.amt; ;
//                 durab = grid.durab;
//             }
//         }
//         public Grid() { }
//         public Grid(int amt = 0, string item = "n", int durab = -1)
//         {
//             this.amt = amt;
//             this.item = item;
//             this.durab = durab;
//         }
//         public override string ToString()
//         {
//             string s = $"{amt},{item},{durab}";
//             return SPack.Paking(s);
//         }
//         public void Parse(string data)
//         {
//             var l = SPack.Depack(data);
//             amt = int.Parse(l[0]);
//             item = l[1];
//             durab = int.Parse(l[2]);
//         }
//     }
//     public void Invchange()
//     {
//         WhenInvChange?.Invoke(this);
//     }
//     /// <summary>
//     ///
//     /// </summary>
//     /// <param name="item"></param>
//     /// <returns>return burability of item if it is a tool</returns>
//     public static int SetTool(string item)
//     {
//         if (Item.IsTool(item))
//             return Item.GetData(item).tool.durability;
//         else return -1;
//     }
//
//     public delegate void Update(Inventory inv);
// }
public class Container
{
	public Container(int cellnum, int cellmax)
	{
		cellCount = cellnum;
		cells = new();
		while (cellnum-- > 0)
		{
			cells.Add(new());
		}
		cellMax = cellmax;
	}

	public List<Cell> cells;
	public int cellMax;
	public int cellCount;

	public int FindVoidCell()
	{
		for (int i = 0; i < cellCount; i++)
		{
			if(cells[i].IsVoid())
				return i;
		}
		return -1;
	}

	public int FindSameCell(SMType type)
	{
		for(int i = 0; i< cellMax; i++)
		{
			if(cells[i].IsSame(type))
				return i;
		}
		return -1;
	}

	public bool Insert(int cellid, SMType type, int quantity, out int remain)
	{
		var cell =  cells[cellid];

		remain = quantity;
		if(!cell.IsVoid()) return false;

		cell.type = type;
		int sum = cell.quantity + quantity;
		if (sum <= cellMax)
		{
			remain = 0;
			cell.quantity = sum;
			return true;
		}
		else
		{
			remain = sum - cellMax;
			cell.quantity = cellMax;
			return false;
		}
	}

	public bool TakeOut(int cellid, SMType type, int quantity, out int remain)
	{
		var cell = cells[cellid];
		remain = quantity;

		int dis =  cell.quantity - quantity;
		if (dis < 0)
		{
			remain = -dis;
			cell.quantity = 0;
			return false;
		}
		else
		{
			remain = 0;
			cell.quantity = dis;
			return true;
		}
	}

	public void Add(SMType type, int quantity, out int remain)
	{
		remain = quantity;
		while (true)
		{
			int id = FindSameCell(type);
			if (id != -1)
			{
				Insert(id, type, quantity, out remain);
				if (remain > 0) continue;
			}
			else //if there are just void cell
			{
				id = FindVoidCell();
				if (id != -1)
				{
					Insert(id, type, quantity, out remain);
					if (remain > 0) continue;
				}
			}
			break;
		}
	}

	public void Remove(SMType type, int quantity, out int remain)
	{
		remain = quantity;
		while (true)
		{
			int id = FindSameCell(type);
			if (id != -1)
			{
				TakeOut(id, type, quantity, out remain);
				if (remain > 0) continue;
			}
			else //if there are just void cell
			{
				id = FindVoidCell();
				if (id != -1)
				{
					TakeOut(id, type, quantity, out remain);
					if (remain > 0) continue;
				}
			}
			break;
		}
	}

	public class Cell
	{
		public SMType type;
		public int quantity;

		public Amount ToAmount() => new Amount
		{
			quantity = quantity,
			type = type
		};

		public bool IsVoid() => quantity < 0;
		public bool IsSame(SMType type) => this.type == type;
	}
}