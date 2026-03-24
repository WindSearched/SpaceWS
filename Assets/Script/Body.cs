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
			structCount += value.structNumber;
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
[Serializable][MemoryPackable][LuaCallCSharp]
public partial class StructState : State
{
	public string type;
	public Loc location;
	public SMType material;
	public float temperature;
	public float mass;

	public SMType _setMaterial
	{
		set
		{
			material = value;
			var md = ct.materials.Get(value.type, value.mod);
			StructData sd;
			if (!ct.structsData.TryGetValue(type, out var value1))
			{
				string s = $"the struct data of '{type}' is not exists";
				ct.log.Write("StructState._setMaterial",s);
				sd = new();
			}
			else
			{
				sd = value1;
			}

			mass = sd.volume * md.density;
		}
	}
	public static implicit operator byte[](StructState s)
	{
		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms, Encoding.UTF8);

		bw.Write(s.type);
		bw.Write(s.bodyIndex);
		bw.Write(s.isStruct);
		bw.Write(s.location);

		return ms.ToArray();
	}

	public static StructState FromBytes(BinaryReader br)
	{
		StructState s = new();
		s.type = br.ReadString();
		s.bodyIndex = br.ReadInt32();
		s.isStruct = br.ReadBoolean();
		s.location = Loc.FromBytes(br);

		return s;
	}
}

[Serializable]
public class StructData
{
	public string type;
	public Demand[] buildDemands;
	public Remove remove;

	public float mass;
	public float volume = -1;

	[Serializable]
	public struct Demand
	{
		public string itemType;
		public int quantity;
	}
	[Serializable]
	public struct Remove
	{
		public float time;
		public Demand[] remains;
	}
	public string mod;
	public Sprite icon;
	public bool externIcon;
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
	public Dictionary<int,body> datas = new();
	/// <summary>
	/// body
	/// </summary>
	public Dictionary<int, obj> objects = new();

	/// <param name="index"></param>
	/// <param name="loc">relative location</param>
	/// <param name="cp"></param>
	public void LoadVoidBody(int index, Loc loc, V3I cp)
	{
		datas.Add(index, new body()
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
		loc.LocateHere(g,cp);
		objects.Add(index, new obj()
		{
			self = g,
			structs = new(),
			faces = new(),
			str = g.transform.GetChild(1).gameObject,
			fc = g.transform.GetChild(0).gameObject,
		});
	}

	/// <param name="cp">relative location</param>
	public GameObject LoadStruct(StructState strct, V3I cp, Material material = null, GameObject strobj = null)
	{
		if(strct.type == null)
			return strobj;
		if (!strobj || strobj.name == "new")
		{
			if(ct.structsInfo.TryGet(strct.type, strct.type, out var info))
				strobj = Object.Instantiate(info.template);
			else
			{
				ct.log.Write("Bodies", $"the struct {strct.type} doesn't exist");
				return null;
			}
		}

		strobj.transform.SetParent(objects[strct.bodyIndex].str.transform);
		strobj.tag = "struct";
		strobj.name = datas[strct.bodyIndex].structs.Count.ToString();

		strct.location.LocateHere(strobj, cp);
		objects[strct.bodyIndex].structs.Add(strobj);
		strobj.SetActive(true);

		//reg
		RegisterStruct(strct, strobj);


		if (strct.material.type == null)
		{
			var t = ct.materials.dict.Values.First().Values.First();
			strct._setMaterial = new(t.type, t.mod);
		}
		var bs = datas[strct.bodyIndex].self;
		var md = ct.materials.Get(strct.material.type, strct.material.mod);
		bs._equilibrateTemperature = new(strct.mass, md.specificHeat, strct.temperature);
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
		int cx, int cy, int cz,
		float rx, float ry, float rz, int index, string type) =>
		LoadStruct(new V3(px, py, pz), new V3I(cx, cy, cz),new V3(rx, ry, rz), index, type);

	public GameObject LoadStruct(V3 pos, V3I cp, V3 rot,int index , string type) =>
		LoadStruct(pos, cp, new Quater(Quaternion.Euler(rot.x, rot.y, rot.z)), index, type);
	public GameObject LoadStruct(V3 pos, V3I cp, Quater rot, int index, string type)
	{
		Loc loc = new Loc
		{
			position = pos,
			rotation = rot
		};
		if (index == -1)
		{
			index = ct.curWorldRule.distribuiteBodyIndex;
			LoadVoidBody(index, loc, cp);
		}
		return LoadStruct(loc, cp, index,type);
	}

	/// <summary>
	/// auto detect if the struct has the material
	/// </summary>
	/// <param name="loc"></param>
	/// <param name="cp"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	public GameObject LoadStruct(Loc loc, V3I cp, int index, string type)
	{
		//Dictionary<string, Material> mats = ct.materials.GetValueOrDefault(type);
		return LoadStruct(new StructState { location = loc, type = type, bodyIndex = index}, cp /*, mat*/);
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
			if(active)
				mr.renderingLayerMask = 3;//00000011
			else
			{
				mr.renderingLayerMask = 1;//00000001
			}
		}
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
		return $"({x},{y},{z})";
	}

	public static V3 operator +(V3 p, V3I ip)
	{
		return new(p.x + ip.x, p.y + ip.y, p.z + ip.z);
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

	public Location ToLocation()
	{
		Location loc = new Location();
		loc.position = position.ToVector3();
		loc.rotation = rotation.ToQuaternion();
		return loc;
	}
	/// <summary>
	/// locate here with relative position
	/// </summary>
	/// <param name="target"></param>
	/// <param name="cp">chunk position</param>
	public void LocateHere(GameObject target, V3I cp)
	{
		var t = target.transform;
		var p = position + cp * ct.setting.chunkUnit;
		t.SetPositionAndRotation(p.ToVector3(), rotation.ToQuaternion());
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
}