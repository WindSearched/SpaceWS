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
	[MemoryPackIgnore][NonSerialized]
	public bool processed;

	public SMType material;
	public float temperature;
	public float mass;

	public int choosedRecipeIndex = 0;
	public bool producing;

	public bool buildable;
	public Mixture mixture;
	public Contain container;
	public Chain chain;
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

	public bool isMixture_ => mixture != null;
	public bool isChain => chain != null;

	[MemoryPackConstructor]
	public StructState()
	{
		if(!type.IsNull())
			PostProcess();
	}
	public StructState(SMType type)
	{
		this.type = type;
		PostProcess();
	}
	public void PostProcess()
	{
		var data = ct.structsInfo.Get(type).data;
		if (data.isContainer_)
		{
			container = new Contain(data,this);
		}

		if (data.isChain_)
		{
			chain = new(data.chainFaces.prefaces.Length, data.chainFaces.profaces.Length);
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

		public bool RemoveStructs(SMType type, int quantity, out List<StructIdPath> idps)
		{
			idps = null;
			if (ContainStructs(type, quantity))
			{
				idps = containStructs[type].GetRange(0, quantity);
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
					RemoveStructs(m.type, m.quantity, out var idps);
					ct.chunkLoader.RemoveStructs(idps);
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
					stuffList = new StuffList(data.container.size);
					container = new Depository(data.container.size,data.factory.recipes[state.choosedRecipeIndex])
					{
						unlockAll = true
					};
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
			remain = null;
			if(list == null) return false;
			remain = new(list);
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
		public bool AddItems(Statistic<SMType> statistic, out Statistic<SMType> remain)
		{
			remain = new();
			bool ret = true;

			foreach (var vp in statistic.dict)
			{
				var type =  vp.Key;
				var quantity = vp.Value;
				if (!AddItem(type, quantity, out var r))
				{
					remain.Add(type, r);
					ret = false;
				}

			}
			return ret;
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

		public bool AddStruct(SMType type, StructIdPath idp)
		{
			if (stuffList == null) return  false;
			stuffList.AddStrut(type, idp);
			return true;
		}

		public bool RemoveStruct(SMType type, out StructIdPath idp) => stuffList.RemoveAStruct(type, out idp);

		public void Update(StructState state, StructData data)
		{
			container.Update(state, data);
		}
	}
	[Serializable][MemoryPackable][LuaCallCSharp]
	public partial class Chain
	{
		/// <summary>
		/// struct that is before of this struct
		/// </summary>
		public StructIdPath[] prechains;
		/// <summary>
		/// struct ths if after of this struct, is the ward of chain
		/// </summary>
		public StructIdPath[] prochains;

		public int GetProCount() => prochains.Length;
		public int GetPreCount() => prechains.Length;

		[MemoryPackConstructor]
		public Chain (){}
		public Chain (int preCount, int proCount)
		{
			prechains = new StructIdPath[preCount];
			Array.Fill(prechains, StructIdPath.nul);
			prochains = new StructIdPath[proCount];
			Array.Fill(prochains, StructIdPath.nul);

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

	public StructIdPath _idPath => new(bodyIndex, structIndex);
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

	public override List<Amount> GetList()
	{
		var list = new List<Amount>();
		foreach (var t in queue)
		{
			list.Add(new());
		}
		return list;
	}

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

	public override bool Remove(int quantity, out int remain, out Statistic<SMType> statistic)
	{
		remain = quantity;
		statistic = new();
		while (remain-- > 0)
		{
			if (RemoveFirst(out var t))
			{
				statistic.Add(t);
			}
			else
				return false;
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
	public override void Update(StructState state, StructData data)
	{
		var c = state.container;
		var procId = c.container.ChooseProChainIndex(state);
		var proc = state.chain.prochains[procId];

		if(!ct.TryGetState(proc, out var st))
			return;
		var pros = st.container;

		if (pros.container.full) return;

		var ta = data.container.convenyor.transportAmount;
		if (Remove(ta, out int remain, out var statist))
		{
			pros.container.SetUnlock(statist.GetKeys(), true);
			pros.AddItems(statist, out _ );
			ct.bodies.Update(st._idPath, data.container.convenyor.timeTicks);
		}

		var precId = c.container.ChoosePreChainIndex(state);
		var prec = state.chain.prechains[precId];
		ct.bodies.Update(prec,data.container.convenyor.timeTicks);
	}

	private int procid, precid;
	public override int ChooseProChainIndex(StructState state)
	{
		int max = state.chain.prochains.Length;
		int r = procid++;
		if (r >= max)
			r = 0;
		return r;
	}
	public override int ChoosePreChainIndex(StructState state)
	{
		int max = state.chain.prechains.Length;
		int r = precid++;
		if (r >= max)
			r = 0;
		return r;
	}
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

	public virtual bool Remove(int quantity, out int remain, out Statistic<SMType> list)
	{
		remain = quantity;
		list = null;

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

	public virtual void Update(StructState state, StructData data)
	{

	}

	public virtual List<Amount> GetList()
	{
		return null;
	}

	public virtual void SetUnlock(SMType tyoe, bool unlocking)
	{

	}
	public virtual void SetUnlock(List<SMType> types, bool unlocking)
	{

	}

	public virtual int ChooseProChainIndex(StructState state)
	{
		return -1;
	}
	public virtual int ChoosePreChainIndex(StructState state)
	{
		return -1;
	}
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

	public static bool operator ==(StructIdPath a, StructIdPath b)
		=> a.bodyIndex == b.bodyIndex && a.structIndex == b.structIndex;
	public static bool operator !=(StructIdPath a, StructIdPath b) => !(a == b);


	public override string ToString()
	{
		return bodyIndex + "/" +  structIndex;
	}
}

public class Statistic<T>
{
	public Dictionary<T, int> dict = new();

	public void Add(T item, int quantity)
	{
		if (!dict.ContainsKey(item))
		{
			dict.Add(item, quantity);
		}
		else
		{
			dict[item] += quantity;
		}

	}
	public void Add(T item) => Add(item, 1);

	public List<T> GetKeys() => dict.Keys.ToList();
}