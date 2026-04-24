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
	public Contain container;
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
			container = new Contain();
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
		public Depository containItems;
		public Dictionary<SMType, List<StructIdPath>> containStructs;// the int value is index of struct

		[MemoryPackConstructor]
		public StuffList(){}
		public StuffList(int size)
		{
			containItems = new();
			containStructs = new();

			containItems.max = size;
		}

		public bool AddItem(SMType type, int quantity) => containItems.Deposites(type, quantity);

		public bool AddItem(SMType type, int quantity, out int remain) => containItems.Deposites(type, quantity, out remain);

		public void AddItems(List<Amount> items)
		{
			foreach (var i in items)
			{
				AddItem(i.type, i.quantity);
			}
		}

		public bool RemoveItem(SMType type, int quantity) => containItems.TakeOut(type, quantity);

		public bool RemoveItem(SMType type, int quantity, out int remain) =>  containItems.TakeOut(type, quantity, out remain);
		public bool ContainItems(SMType type, int quantity) => containItems.Contains(type, quantity);

		public bool ContainItem(SMType type) => containItems.Contains(type);

		public void AddStrut(SMType type, int sid, int bid) => AddStrut(type, new StructIdPath(bid, sid));
		public void AddStrut(SMType type, StructIdPath idp)
		{
			if (containStructs.ContainsKey(type))
			{
				containStructs[type].Add(idp);
			}
			else
			{
				containStructs.Add(type, new(){idp});
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

		public bool RemoveAStruct(SMType type, out StructIdPath idp)
		{
			idp = StructIdPath.nul;
			if (!ContainStruct(type)) return false;
		 	idp = containStructs[type].First();
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
	public partial class Contain
	{
		public StuffList stuffList;
		public Container container;

		[MemoryPackConstructor]
		public Contain(){}
		public Contain(StructData data, StructState state)
		{
			if(!data.isContainer_) return;
			var c = data.container;

			switch (c.tag)
			{
				case Container.Tag.factory:
					stuffList = new StuffList(data.factory.size);
					container = new Depository(data.factory.size,data.factory.recipes[state.choosedRecipeIndex]);
					break;
				case Container.Tag.depository:
					container = new Depository(data.container.size);
					break;
				case Container.Tag.cacheQueue:
					container = new CacheQueue(c.count);
					break;
			}
		}

		public bool AddItem(SMType type, int quantity, out int remain) => container.Add(type, quantity, out remain);

		public bool AddItems(List<Amount> list, out List<Amount> remain)
		{
			remain = list;
			foreach (var e in list)
			{
				var rs = remain.First();
				if (AddItem(e.type, e.quantity, out rs.quantity))
				{
					remain.RemoveAt(0);
				}
				else
				{
					return false;
				}
			}
			return true;
		}
		public bool RemoveItem(SMType type, int quantity, out int remain) => container.Remove(type, quantity, out remain);

		public bool AddStuff(SMType type, int quantity, out int remain)
		{
			remain = quantity;
			if (stuffList == null) return false;
			return stuffList.AddItem(type, quantity, out remain);
		}
		public bool RemoveStuff(SMType type, int quantity, out int remain)
		{
			remain = quantity;
			if (stuffList == null) return false;
			return stuffList.RemoveItem(type, quantity, out remain);
		}

		public void AddStruct(SMType type, StructIdPath idp)
		{
			stuffList.AddStrut(type, idp);
		}

		public bool RemoveStruct(SMType type, out StructIdPath idp) => stuffList.RemoveAStruct(type, out idp);
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
[Obsolete("the full value can not fixed")]
public class Inventory : Container
{
	public Inventory(int cellnum, int cellmax)
	{
		count = cellnum;
		cells = new();
		while (cellnum-- > 0)
		{
			cells.Add(new());
		}
		max = cellmax;
	}

	public List<Cell> cells;

	public int FindVoidCell()
	{
		for (int i = 0; i < count; i++)
		{
			if(cells[i].IsVoid())
				return i;
		}
		return -1;
	}

	public int FindSameCell(SMType type)
	{
		for(int i = 0; i< max; i++)
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
		if (sum <= max)
		{
			remain = 0;
			cell.quantity = sum;
			return true;
		}
		else
		{
			remain = sum - max;
			cell.quantity = max;
			return false;
		}
	}

	public bool TakeOut(int cellid, int quantity, out int remain)
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

	public override bool Add(SMType type, int quantity, out int remain)
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
				else
				{
					return false;
				}
			}
			break;
		}
		return true;
	}

	public override bool Remove(SMType type, int quantity, out int remain)
	{
		remain = quantity;
		while (true)
		{
			int id = FindSameCell(type);
			if (id != -1)
			{
				TakeOut(id, quantity, out remain);
				if (remain > 0) continue;
			}
			else //if there are just void cell
			{
				id = FindVoidCell();
				if (id != -1)
				{
					TakeOut(id, quantity, out remain);
					if (remain > 0) continue;
				}
				else
				{
					return false;
				}
			}
			break;
		}
		return true;
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


public class CacheQueue : Container
{
	public Queue<SMType> queue = new();

	public CacheQueue(int count)
	{
		tag = Tag.cacheQueue;
		max = 1;
		this.count = count;
	}
	public override bool Add(SMType type)
	{
		if (queue.Count >= count)
		{
			full = true;
			return false;
		};
		full = false;
		queue.Enqueue(type);
		return true;
	}

	public override bool Add(SMType type, int quantity, out int remain)
	{
		remain = quantity;
		while (remain-- > 0)
		{
			if(!Add(type)) return false;
		}
		return true;
	}

	/// <summary>
	/// check the first item is this type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public override bool Remove(SMType type)
	{
		if(queue.Count >= count || queue.First() != type)
		{
			full = true;
			return false;
		}
		full = false;
		queue.Dequeue();
		return true;
	}

	public override bool Remove(SMType type, int quantity, out int remain)
	{
		remain = quantity;
		while (remain-- > 0)
		{
			if(!Remove(type)) return false;
		}
		return true;
	}

	public override bool RemoveFirst(out SMType type)
	{
		type = new();
		if (queue.Count == 0) return false;
		type = queue.Dequeue();
		return true;
	}

	public override bool IsFull(SMType _) => full;
}

[Serializable][MemoryPackable][LuaCallCSharp]
public partial class Container
{
	public bool full;
	public int max;
	public int count;
	public Tag tag;

	public virtual bool Add(SMType type)
	{
		full = true;
		return false;
	}

	public virtual bool Add(SMType type, int quantity, out int remain)
	{
		full = true;
		remain = quantity;
		return false;
	}

	public virtual bool Remove(SMType type)
	{
		full = true;
		return false;
	}
	public virtual bool Remove(SMType type, int quantity, out int remain)
	{
		full = true;
		remain = quantity;
		return false;
	}

	public virtual bool RemoveFirst(out SMType type)
	{
		full = true;
		type = new();
		return false;
	}

	public virtual bool IsFull(SMType tyoe) => true;

	public enum Tag
	{
		nul,inventory, depository, cacheQueue, cacheStack, factory
	}
	public bool Is(Tag tag) => this.tag == tag;
}

public struct StructIdPath
{
	public int bodyIndex;
	public int structIndex;

	public bool IsNull() => bodyIndex == -1 || structIndex == -1;

	public StructIdPath(int bid, int sid)
	{
		bodyIndex = bid;
		structIndex = sid;
	}
	public static StructIdPath nul = new(){ bodyIndex = -1, structIndex = -1 };
}