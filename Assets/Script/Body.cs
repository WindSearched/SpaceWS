using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;
using XLua;
using Object = UnityEngine.Object;

[Serializable]
public class Body
{
	public int index;
	public Loc location;
	public List<V3> points;

}
/// <summary>
/// generic state
/// </summary>
[Serializable]
public class State
{
	public string type = "";
	public int bodyIndex;
	public bool isStruct;

}

[Serializable][MemoryPackable][LuaCallCSharp]
public partial class BodyState
{
	public int index;
	public Loc location;
	public int structCount;

	public float sumSpecif1icHeat;
	public float averageSpecif1icHeat;
	public float temperature;
	/// <summary>
	/// total mass in the body
	/// </summary>
	public float mass = 1;

	public float minFusionPoint;
	public int minFusionPointStructIndex;

	/// <summary>
	/// setonly, add the specific heat to body
	/// </summary>
	[MemoryPackIgnore]
	public (float specifcHeat, int structNumber) _addSpacificHeat
	{
		set
		{
			sumSpecif1icHeat += value.specifcHeat;
			averageSpecif1icHeat = sumSpecif1icHeat / structCount;
		}
	}
	/// <summary>
	/// input mass, specificHeat and temperature
	/// </summary>
	[MemoryPackIgnore]
	public (float mass, float specificHeat, float temperature) _equilibrateTemperature
	{
		set => temperature = (mass * averageSpecif1icHeat * temperature + //find equilibrate temperature
		                      value.mass * value.specificHeat * value.temperature) /
		                     (mass * averageSpecif1icHeat + value.mass * value.specificHeat);
	}

	public float _addHeat
	{
		set
		{
			if(mass == 0 || value == 0)
				return;
			if(float.IsNaN(temperature))
				temperature = 0;
			temperature += value / (averageSpecif1icHeat * mass);
		}
	}

	public int getNewStructIndex_ => curstrid++;

	public int curstrid;
	public static implicit operator byte[](BodyState s)
	{
		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms, Encoding.UTF8);

		bw.Write(s.index);
		bw.Write(s.location);

		return ms.ToArray();
	}

	public static BodyState FromBytes(BinaryReader br)
	{
		BodyState s = new();
		s.index = br.ReadInt32();
		s.location = Loc.FromBytes(br);
		return s;
	}
}


[Serializable]
public class StructData
{
	public string type;
	public float volume = -1;
	public Factory factory;
	public Contain container;
	public ChainFaces chainFaces;

	[Serializable]
	public class Factory
	{
		public float heatRelease;
		public List<Recipe> recipes = new();
		public int size;

		[Serializable]
		public class Recipe
		{
			public List<Amount> materials;
			public List<Amount> structMaterials;
			public List<Amount> products;
			public float productionTime;

		}
	}
	[Serializable]
	public class Contain
	{
		public Container.Tag tag;
		public int count;
		public int size;
		public Convenyor convenyor;
	}

	public class ChainFaces
	{
		public int[] profaces;
		public int[] prefaces;

		/// <returns>1 is proface; 0 is not chain face; -1 is preface</returns>
		public int Parse(int faceid, out int parsed)
		{
			if (prefaces.Contains(faceid))
			{
				parsed = -1;
				return Array.IndexOf(prefaces, faceid);
			}
			if(profaces.Contains(faceid))
			{
				parsed = 1;
				return Array.IndexOf(profaces, faceid);
			}
			else
			{
				parsed = 0;
				return 0;
			}
		}
	}
	public class Convenyor
	{
		/// <summary>
		/// ticks of a time
		/// </summary>
		public int timeTicks;
		/// <summary>
		/// amount to transport in a time
		/// </summary>
		public int transportAmount = 1;
	}

	public bool isFactory_ => factory != null && factory.recipes.Count > 0;
	public bool isContainer_ => container != null;
	public bool isChain_ => chainFaces != null;
	public bool isConvaenyor_ => isContainer_ && container.convenyor != null;

	public string mod;
	public Sprite icon;
	public bool externIcon;

	public SMType smt => new(type, mod);
}
[Serializable]
public class FaceState :State
{
	public int points;
	public List<int> pointIndexs;
}

public class Bodies
{
	public struct body
	{
		public List<StructState> structs;
		public List<FaceState> faces;
		public BodyState self;
	}

	public struct obj
	{
		public GameObject self;
		public GameObject str;
		public GameObject fc;
		public List<GameObject> structs;
		public List<GameObject> faces;
	}

	public Dictionary<int, body> datas = new();

	/// <summary>
	/// body
	/// </summary>
	public Dictionary<int, obj> objects = new();
	/// <param name="index"></param>
	/// <param name="loc">absolute location</param>
	public void LoadVoidBody(int index, Loc loc)
	{
		if (datas.ContainsKey(index))
		{

		}

		datas.TryAdd(index, new body()
		{
			structs = new(),
			faces = new(),
			self = new()
			{
				location = loc
			}
		});
		var g = GameObject.Instantiate(ct.defualtBody, ct.bodiesParent);
		g.name = index.ToString();
		loc.LocateHere(g);
		objects.TryAdd(index, new obj()
		{
			self = g,
			structs = new(),
			faces = new(),
			str = g.transform.GetChild(1).gameObject,
			fc = g.transform.GetChild(0).gameObject,
		});
	}

	public GameObject LoadStruct(StructState strct, Material material = null, GameObject strobj = null)
	{
		if (strct.type.IsNull())
			return strobj;
		if (!strobj || strobj.name == "new")
		{
			if (ct.structsInfo.TryGet(strct.type, out var info))
				strobj = Object.Instantiate(info.template);
			else
			{
				ct.log.Write("Bodies", $"the struct {strct.type} doesn't exist");
				return null;
			}
		}

		var bs = datas[strct.bodyIndex].self;

		strct.structIndex = bs.getNewStructIndex_; //registries index of struct

		strobj.transform.SetParent(objects[strct.bodyIndex].str.transform);
		strobj.tag = "struct";
		strobj.name = strct.structIndex.ToString();

		strct.Locate(strobj);
		objects[strct.bodyIndex].structs.Add(strobj);
		strobj.SetActive(true);

		//reg
		RegisterStruct(strct, strobj);


		if (strct.material.type == null)
		{
			var t = ct.materials.dict.Values.First().Values.First();
			strct._setMaterial = new(t.type, t.mod);
		}

		var md = ct.materials.Get(strct.material.type, strct.material.mod);
		bs._equilibrateTemperature = new(strct.mass, md.specificHeat, strct.temperature);
		bs.structCount++;
		bs._addSpacificHeat = new(md.specificHeat, 1);
		bs.mass += strct.mass;


		return strobj;
	}

	public void RegisterStruct(StructState state, GameObject structObject)
	{
		ct.bodies.objects[state.bodyIndex].structs.Add(structObject);
		ct.bodies.datas[state.bodyIndex].structs.Add(state);
	}

	public GameObject LoadStruct(float px, float py, float pz,
		float rx, float ry, float rz, int index, string type, string mod) =>
		LoadStruct(new V3(px, py, pz), new V3(rx, ry, rz),
			index, new(type, mod));

	public GameObject LoadStruct(V3 pos, V3 rot, int index, SMType type) =>
		LoadStruct(pos, new Quater(Quaternion.Euler(rot.x, rot.y, rot.z)), index, type);

	public GameObject LoadStruct(V3 pos, Quater rot, int index, SMType type)
	{
		Loc loc = new Loc
		{
			position = pos,
			rotation = rot
		};
		if (index == -1)
		{
			index = ct.curWorldRule.distribuiteBodyIndex;
			LoadVoidBody(index, loc);
		}

		return LoadStruct(loc, index, type);
	}

	/// <summary>
	/// auto detect if the struct has the material
	/// </summary>
	/// <param name="loc"></param>
	/// <param name="cp"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	public GameObject LoadStruct(Loc loc, int index, SMType type)
	{
		var s = new StructState(type) { _absLoc = loc, bodyIndex = index };
		s.PostProcess();
		return LoadStruct(s /*, mat*/);
	}


	public GameObject RemoveStruct(int body, int index)
	{
		var g = objects[body].structs[index];
		objects[body].structs[index] = null;
		return g;
	}

	public void AddHeat(int index, float heat)
	{
		var bs = datas[index].self;
		bs._addHeat = heat;

		Debug.Log(bs.temperature);

	}

	/// <summary>
	/// Render for gameobject its outline
	/// </summary>
	/// <param name="g"></param>
	public static void OutlineObj(GameObject g, bool active)
	{
		if (g.TryGetComponent<MeshRenderer>(out var mr))
		{
			if (active)
				mr.renderingLayerMask = 3; //00000011
			else
			{
				mr.renderingLayerMask = 1; //00000001
			}
		}
	}

	/// <summary>
	/// adsorb a struct on a other
	/// </summary>
	/// <param name="adsorber">data of struct as the anchor</param>
	/// <param name="adsorbed">data of struct adsorbed on the anchor struct</param>
	public void Adsorption((StructState state, int face, GameObject obj) adsorbed,
		(GameObject obj, int bid, int sid, int face) adsorber, bool onlyAdssorb = false)
	{
		if (!adsorbed.obj)
			LoadStruct(adsorbed.state, strobj: adsorbed.obj);

		SMesh.Face.AlignFaceToFace(adsorbed.obj, ct.structsInfo.Get(adsorbed.state.bodyIndex).faces, adsorbed.face
			, adsorber.obj, ct.structsInfo.Get(adsorber.bid).faces, adsorber.face);

		if (!onlyAdssorb)
		{
			AdsorptionPostProcess(adsorbed, adsorber);
		}
	}

	/// <summary>
	/// void AdsorptionPostProcess((StructState state, int face, GameObject obj) adsorbed, (GameObject obj, int bid, int sid, int face) adsorber)
	/// </summary>
	/// <param name="adsorbed"></param>
	/// <param name="adsorber"></param>
	public void AdsorptionPostProcess((StructState state, int face, GameObject obj) adsorbed,
		(GameObject obj, int bid, int sid, int face) adsorber)
	{
		adsorbed.state.bodyIndex = adsorber.bid; //transfer the struct to adsorber
		adsorbed.obj.name = (adsorber.sid + 1).ToString();
		adsorbed.obj.transform.SetParent(adsorber.obj.transform.parent);

		//equilibrating temperature
		var edb = datas[adsorbed.state.bodyIndex].self;
		var erb = datas[adsorber.bid].self;
		//adsorbed.state.structIndex = erb.getNewStructIndex_;
		edb.structCount--;
		var ermd = ct.materials.Get(adsorbed.state.material);
		erb._equilibrateTemperature = new(adsorbed.state.mass, ermd.specificHeat, adsorbed.state.temperature);
		erb.structCount++;
		erb._addSpacificHeat = new(ermd.specificHeat, 1);
		erb.mass += adsorbed.state.mass;


		var erstate = datas[adsorber.bid].structs[adsorber.sid];
		var erdata = ct.structsInfo.Get(erstate.type).data;

		if (erdata.isFactory_ && erdata.isContainer_)
		{
			erstate.container.AddStruct(adsorbed.state.type, new(adsorbed.state.bodyIndex, adsorbed.state.structIndex));
			TryProduction(erstate, erdata);
		}
	}

	/// <summary>
	/// try to product
	/// </summary>
	public void TryProduction(int bid, int sid)
	{
		var state = datas[bid].structs[sid];
		var data = ct.structsInfo.Get(state.type).data;

		if (!data.isFactory_) return;
		TryProduction(state, data);
	}

	public void TryProduction(StructState state, StructData data) =>
		Tick.Reg(_ => Producing(state, data),
			(int)(ct.setting.tickPerSecond * data.factory.recipes[state.choosedRecipeIndex].productionTime));

	public void Producing(StructState state, StructData data)
	{
		var recipe = data.factory.recipes[state.choosedRecipeIndex];
		if (state.container.stuffList.TryRemove(recipe))
		{
			if (state.container.AddItems(recipe.products, out var list))
			{
				Tick.Reg(_ => Producing(state, data), (int)(ct.setting.tickPerSecond * recipe.productionTime));
			}
		}

		state.producing = false;
	}

	public Updater defaultUpdater = (idp, state, data) => {

	};

	/// <param name="tick">after this tick execute</param>
	public void Update(StructIdPath idp, int tick)
	{
		Tick.Reg(_ =>
		{
			var s = ct.GetState(idp);
			var d = ct.GetData(s);

			if (d.isFactory_)
			{

			}

			if (d.isContainer_)
			{
				s.container.Update(s,d);
			}

			if (idp == ct.viewPage.targetPath)
			{
				ct.viewPage.Refresh();
			}
		}, tick);
	}

	/// <summary>
	/// Load a struct on a face of struct
	/// </summary>
	/// <param name="idp">index path of connected</param>
	/// <param name="state">state not load of loaded</param>
	/// <param name="faceId">face to connect of connected</param>
	/// <param name="ldFace">face to connect of loaded</param>
	public void LoadStructOn(StructState state, int faceId, StructState based, int ldFace)
	{
		if (state.type.IsNull())
			return;
		var ng = LoadStruct(state);
		var t = ct.buildPage.buildObject.transform;
		ng.transform.SetPositionAndRotation(t.position, t.rotation);

		var idp = state._idPath;
		var b = Concatenating(based, faceId, state, ldFace);
		Debug.Log(b);

		// AdsorptionPostProcess(new(state, ldFace, ng),
		// 	new(objects[idp.bodyIndex].structs[idp.structIndex], idp.bodyIndex, idp.structIndex, faceId));
	}

	public void Concatenating(StructState state, int idpre, int idpro,
		StructState prestate = null, int preIdpro = -1,
		StructState prostate = null, int proIdpre = -1)
	{
		if (state != null && state.isChain)
		{
			if (idpre != -1 && idpre < state.chain.GetPreCount())
				state.chain.prechains[idpre] = prestate._idPath;
			if(idpro != -1 && idpro < state.chain.GetProCount())
				state.chain.prochains[idpro] = prostate._idPath;
		}

		if (prestate != null && prestate.isChain && preIdpro != -1 && preIdpro <= state.chain.GetPreCount())
			prestate.chain.prochains[preIdpro] = state._idPath;

		if (prostate != null && prostate.isChain && proIdpre != -1 && proIdpre <= state.chain.GetProCount())
			prostate.chain.prechains[proIdpre] = state._idPath;
	}

	public bool Concatenating(StructState connected, int connectedFace, StructState connector, int connectorFace)
	{
		var connectorData = ct.GetData(connector);
		var connectedData = ct.GetData(connected);

		if (!connectorData.isChain_ || !connectedData.isChain_) return false;
		int orid = connectorData.chainFaces.Parse(connectorFace, out var parsed);
		int edid = connectedData.chainFaces.Parse(connectedFace, out var i);
		if (parsed == 1)
		{
			if(i != -1) return false;
			Concatenating(connector, -1, orid, prostate: connected, proIdpre: edid);
		}
		else if (parsed == -1)//face is pre
		{
			if(i != 1) return false;
			Concatenating(connector, orid, -1, prestate: connector, preIdpro: edid);
		}
		return true;
	}
}

/// <summary>
/// the struct of position and rotation
/// </summary>
public struct Location
{
	public Vector3 position;
	public Quaternion rotation;

	public Loc ToLoc()
	{
		return new Loc(this);
	}
}
[MemoryPackable][LuaCallCSharp]
public partial struct V3
{
	public float x;
	public float y;
	public float z;
	public V3(Vector3 v)
	{
		x = v.x;
		y = v.y;
		z = v.z;
	}

	public V3(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}
	public Vector3 ToVector3()
	{
		return new Vector3(x, y, z);
	}

	public V3I intDivision(int n) => new((int)x / n, (int)y / n, (int)z / n);

	public static V3 zero = new() {
		x = 0, y = 0, z = 0
	};

	public override string ToString()
	{
		return $"({x};{y};{z})";
	}

	public static V3 Parse(string s)
	{
		s = s.TrimStart('(');
		s = s.TrimEnd(')');

		var ss = s.Split(";");
		var a = float.Parse(ss[0]);
		var b = float.Parse(ss[1]);
		var c = float.Parse(ss[2]);

		return new V3(a, b, c);
	}

	public static bool TryParse(string s, out V3 v)
	{
		try
		{
			v = Parse(s);
			return true;
		}
		catch
		{
			v = default;
			return false;
		}
	}
	public static V3 operator +(V3 p, V3I ip)
	{
		return new(p.x + ip.x, p.y + ip.y, p.z + ip.z);
	}
	public static V3 operator +(V3 p, V3 pp)
	{
		return new(p.x + pp.x, p.y + pp.y, p.z + pp.z);
	}

	public static V3 operator *(V3 p, float f)
	{
		return new(p.x * f, p.y * f, p.z * f);
	}
	public static V3 operator /(V3 p, float n)
	{
		return new(p.x / n, p.y / n, p.z / n);
	}

	public static V3 operator %(V3 p, float n)
	{
		return new(p.x % n, p.y % n, p.z % n);
	}

	public static implicit operator byte[](V3 p)
	{
		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms, Encoding.UTF8);

		bw.Write(p.x);
		bw.Write(p.y);
		bw.Write(p.z);

		return ms.ToArray();
	}

	public static V3 FromBytes(BinaryReader br)
	{
		V3 v = new V3();
		v.x = br.ReadSingle();
		v.y = br.ReadSingle();
		v.z = br.ReadSingle();

		return v;
	}
}

[MemoryPackable][LuaCallCSharp]
public partial struct V3I
{
	public int x;
	public int y;
	public int z;
	public V3I(Vector3Int v)
	{
		x = v.x;
		y = v.y;
		z = v.z;
	}
	public V3I(int ix,int iy,int iz)
	{
		x = ix;
		y = iy;
		z = iz;
	}
	public Vector3Int ToVector3Int()
	{
		return new Vector3Int(x, y, z);
	}

	public void Set(Vector3 p)
	{
		x = SMath.Floor(p.x);
		y = SMath.Floor(p.y);
		z = SMath.Floor(p.z);
	}

	public void Set(Vector3Int p)
	{
		x = p.x;
		y = p.y;
		z = p.z;
	}

	public void Add(int x, int y, int z)
	{
		this.x += x;
		this.y += y;
		this.z += z;
	}

	public V3I Addition(int x, int y, int z)
	{
		return new V3I(this.x + x, this.y + y, this.z + z);
	}

	public static V3I zero = new() {
		x = 0, y = 0, z = 0
	};

	public override string ToString() => $"({x},{y},{z})";

	public static V3I operator +(V3I a, V3I b)
	{
		return new V3I(a.x + b.x, a.y + b.y, a.z + b.z);
	}
	public static V3I operator /(V3I a, int b)
	{
		return new V3I(a.x / b, a.y / b, a.z / b);
	}
	public static V3I operator -(V3I a, V3I b)
	{
		return new V3I(a.x - b.x, a.y - b.y, a.z - b.z);
	}
	public static V3I operator *(V3I a, int i)
	{
		return new V3I(a.x * i, a.y * i, a.z * i);
	}
	public static bool operator ==(V3I a, V3I b) => a.x == b.x && a.y == b.y && a.z == b.z;
	public static bool operator !=(V3I a, V3I b) => !(a == b);
}

[LuaCallCSharp]
public struct Quater
{
   public float x;
	public float y;
	public float z;
	public float w;
	public Quater(Quaternion q)
	{
		x = q.x;
		y = q.y;
		z = q.z;
		w = q.w;
	}

	public Quater(float rx, float ry, float rz)
	{
		var q = Quaternion.Euler(rx,ry,rz);
		x = q.x;
		y = q.y;
		z = q.z;
		w = q.w;
	}
	public Quater(V3 rot)
	{
		var q = Quaternion.Euler(rot.x, rot.y, rot.z);
		x = q.x;
		y = q.y;
		z = q.z;
		w = q.w;
	}

	public Quaternion ToQuaternion()
	{
		return new Quaternion(x, y, z, w);
	}

	public static Quater zero = new()
	{
		x = 0, y = 0,z = 0,w = 0
	};

	public static Quater Parse(V3 rot) => new(rot);

	public override string ToString()
	{
		return $"({x},{y},{z},{w})";
	}


	public static implicit operator byte[](Quater q)
	{
		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms, Encoding.UTF8);

		bw.Write(q.x);
		bw.Write(q.y);
		bw.Write(q.z);
		bw.Write(q.w);

		return ms.ToArray();
	}

	public static Quater FromBytes(BinaryReader br)
	{
		var q = new Quater();
		q.x = br.ReadSingle();
		q.y = br.ReadSingle();
		q.z = br.ReadSingle();
		q.w = br.ReadSingle();

		return q;
	}
}
/// <summary>
/// location to save
/// </summary>
[LuaCallCSharp]
public struct Loc
{
	public V3 position;
	public Quater rotation;

	public Loc(Location loc)
	{
		position = new V3(loc.position);
		rotation = new Quater(loc.rotation);
	}

	public Loc(float x, float y, float z, float rx, float ry, float rz)
	{
		position = new V3(x, y, z);
		rotation = new Quater(rx, ry, rz);
	}

	public Loc(V3 pos, V3 rot)
	{
		position = pos;
		rotation = new Quater(rot);
	}
	public Loc(V3 pos, Quater rot)
	{
		position = pos;
		rotation = rot;
	}

	public Location ToLocation()
	{
		Location loc = new Location();
		loc.position = position.ToVector3();
		loc.rotation = rotation.ToQuaternion();
		return loc;
	}

	/// <summary>
	/// locate here with absolutely position
	/// </summary>
	/// <param name="target"></param>
	public void LocateHereAbs(GameObject target)
	{
		var t = target.transform;
		t.SetPositionAndRotation(position.ToVector3(), rotation.ToQuaternion());
	}

	public static Loc zero = new() { position = V3.zero,rotation=Quater.zero};
	public override string ToString()
	{
		return $"({position}, {rotation})";
	}

	public static implicit operator byte[](Loc loc)
	{
		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms, Encoding.UTF8);

		bw.Write(loc.position);
		bw.Write(loc.rotation);

		return ms.ToArray();
	}

	public static Loc FromBytes(BinaryReader br)
	{
		Loc l = new();
		l.position = V3.FromBytes(br);
		l.rotation = Quater.FromBytes(br);

		return l;
	}

	public void LocateHere(GameObject target)
	{
		var t = target.transform;
		t.SetPositionAndRotation(position.ToVector3(), rotation.ToQuaternion());
	}
}
[Serializable]
public class ItemData
{
	public string type;

    [JsonIgnore]
    public string mod;
	[JsonIgnore]
	public Sprite sprite;
	[JsonIgnore]
	public GameObject template;
}
[Serializable]
public class MaterialData
{
	public string type;
	public float fusionPoint;
	public float specificHeat;
	public float density;

	[JsonIgnore] public string mod;

	public SMType smt => new(type, mod);
}

public struct SPos
{
	public V3 position;
	public V3 offset;
	public float power;

	public V3 Get() => position * power + offset;

	public SPos(V3 pos, V3 offset, float power)
	{
		position = pos;
		this.offset = offset;
		this.power = power;
	}

	public SPos(V3 pos, V3 offset)
	{
		position = pos;
		this.offset = offset;
		power = ct.setting.chunkUnit;
	}

	public SPos(V3 offset)
	{
		this.offset = offset;
		position = V3.zero;
		power = 0;
	}
}
[Serializable]
public struct Amount
{
	public SMType type;
	public int quantity;

	public Amount(SMType type)
	{
		this.type = type;
		quantity = 1;
	}

	public Amount(SMType type, int quantity)
	{
		this.type = type;
		this.quantity = quantity;
	}

	public override string ToString() => $"({quantity} : {type})";
}
[Serializable][MemoryPackable][LuaCallCSharp]
public partial class Depository : Container
{
	public Dictionary<SMType, int> cells = new();
	public List<SMType> unlocks = new();
	public bool unlockAll;

	[MemoryPackConstructor]
	public Depository(){}

	public Depository(int size)
	{
		max = size;
	}

	public Depository(int size, StructData.Factory.Recipe recipe)
	{
		max = size;
		UnlockAdapt(recipe.materials);
	}

	public override List<Amount> GetList()
	{
		return (from unlock in unlocks let quantity = cells[unlock] select new Amount(unlock, quantity)).ToList();
	}

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

	public override bool Add(SMType type, int quantity, out int remain)
	{
		remain = quantity;
		if (!unlockAll && !unlocks.Contains(type)) return false;
		if (!Contains(type))
		{
			cells.Add(type, 0);
		}
		int sum =  cells[type] + quantity;
		if (sum > max)
		{
			remain = sum - max;
			cells[type] = max;
			return false;
		}
		else
		{
			remain = 0;
			cells[type] = sum;
			return true;
		}
	}
	public bool Deposites(SMType type, int quantity, out int remain) => Add(type, quantity, out remain);

	public bool TakeOut(SMType type, int quantity)
	{
		if(!unlockAll && !unlocks.Contains(type) && !Contains(type, quantity)) return false;
		cells[type] -= quantity;
		return true;
	}

	public bool TakeOut(SMType type, int quantity, out int remain) => Remove(type, quantity, out remain);

	public override bool Remove(SMType type, int quantity, out int remain)
	{
		remain = quantity;
		if (!unlockAll && !unlocks.Contains(type)) return false;
		int dis = cells[type] - quantity;
		if (dis > 0)
		{//over
			remain = -dis;
			cells[type] = 0;
			return false;
		}
		else
		{
			remain = 0;
			cells[type] = dis;
			return true;
		}
	}

	public override bool Remove(int quantity, out int remain, out Statistic<SMType> stat)
	{
		stat = new Statistic<SMType>();
		foreach (var unlock in unlocks)
		{
			var q = cells[unlock];
			if(q > quantity)
			{
				stat.Add(unlock, quantity);
				Remove(unlock, quantity, out _);
				remain = q-quantity;
				return false;
			}
			else
			{
				stat.Add(unlock, q);
				quantity -= q;
				Set(unlock, 0);
				if (quantity == 0)
				{
					remain = 0;
					return true;
				}
			}
		}
		remain = quantity;
		return false;
	}

	private void Set(SMType type, int quantity)
	{
		if (cells.ContainsKey(type))
		{
			cells[type] = quantity;
		}
	}

	public bool Contains(SMType type) => cells.ContainsKey(type);

	public bool Contains(SMType type, int quantity) => Contains(type) && cells[type] >= quantity;

	public override bool IsFull(SMType type) => Contains(type) && cells[type] >= max;

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

	public bool RemoveFirst(out SMType type)
	{
		type = new();
		foreach (var unlock in unlocks)
		{
			if (!Contains(unlock, 1)) continue;
			if (!Remove(unlock, 1, out _)) continue;
			type = unlock;
			return true;
		}
		return false;
	}

	public override void Update(StructState state, StructData data)
	{
		var c = state.container;
		var procId = c.container.ChooseProChainIndex(state);
		if (procId != -1)
		{
			var proc = state.chain.prochains[procId];

			if(!ct.TryGetState(proc, out var st))
				return;
			var procs = st.container;

			var ta = data.container.convenyor.transportAmount;
			Remove(ta, out var remain, out var stat);
			if (ta > remain)
			{
				procs.container.SetUnlock(stat.GetKeys(), true);
				if (procs.AddItems(stat, out _))
				{
					ct.bodies.Update(proc, data.container.convenyor.timeTicks);
				}
			}

		}

		var precId = c.container.ChoosePreChainIndex(state);
		if (precId != -1)
		{
			var prec = state.chain.prechains[precId];
			ct.bodies.Update(prec, data.container.convenyor.timeTicks);
		}
	}

	public override void SetUnlock(SMType tyoe, bool unlocking)
	{
		if (unlocks.Contains(tyoe))
		{
			if(!unlocking)
				unlocks.Add(tyoe);
		}
		else
		{
			if(unlocking)
				unlocks.Add(tyoe);
		}
	}

	public override void SetUnlock(List<SMType> types, bool unlocking)
	{
		foreach (var t in types)
		{
			SetUnlock(t, unlocking);
		}
	}

	public int proci;
	public int preci;
	public override int ChooseProChainIndex(StructState state)
	{
		if(!state.isChain) return -1;
		var c = state.chain;

		for (int i = 0; i < c.prochains.Length; i++)
		{
			var id = c.prochains[i];
			if(id.IsNull()) continue;
			var s = ct.GetState(id);
			if(s.container.container.full) continue;
			return i;
		}
		return -1;
	}
	public override int ChoosePreChainIndex(StructState state)
	{
		if(!state.isChain) return -1;
		var c = state.chain;

		for (int i = 0; i < c.prechains.Length; i++)
		{
			var id = c.prechains[i];
			if(id.IsNull()) continue;
			var s = ct.GetState(id);
			if(s.container.container.full) continue;
			return i;
		}
		return -1;
	}
}

public delegate void Updater(StructIdPath idp, StructState state, StructData data);