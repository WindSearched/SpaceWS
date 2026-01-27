using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UIElements;

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

[Serializable]
public class BodyState
{
    public int index;
    public Loc location;
}
[Serializable]
public class StructState : State
{
    public Loc location;
}

[Serializable]
public class StructData
{
    public string meshType;

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
    public void LoadVoidBody(int index, Loc loc)
    {
        datas.Add(index, new body()
        {
            structs = new(),
            faces = new()
        });
        var g = GameObject.Instantiate(ct.defualtBody, ct.bodiesParent);
        g.name = index.ToString();
        loc.LocateHere(g);
        objects.Add(index, new obj()
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
        Debug.Log($"{strct.type},{strct.location.ToString()},{ct.meshFaces.ContainsKey(strct.type)}");

        if (!datas.ContainsKey(strct.bodyIndex))
        {
            return null;
            //LoadVoidBody(strct.bodyIndex);
        }
        datas[strct.bodyIndex].structs.Add(strct);

        if (strobj == null)
        {
            strobj = new GameObject(datas[strct.bodyIndex].structs.Count.ToString());
        }

        SMesh.AddMesh(strobj, ct.meshTypes[strct.type]);//it is test just
        strobj.transform.SetParent(objects[strct.bodyIndex].str.transform);
        objects[strct.bodyIndex].structs.Add(strobj);

        strct.location.LocateHere(strobj);
        return strobj;
    }

    public GameObject LoadStruct(float px, float py, float pz, float rx, float ry, float rz,string type) => LoadStruct(new V3(px,py,pz),new V3(rx,ry,rz),type);

    public GameObject LoadStruct(V3 pos, V3 rot, string type) =>
        LoadStruct(pos, new Quater(Quaternion.Euler(rot.x, rot.y, rot.z)), type);
    public GameObject LoadStruct(V3 pos, Quater rot, string type)
    {
        Loc loc = new Loc
        {
            position = pos,
            rotation = rot
        };
        return LoadStruct(loc,type);
    }

    public GameObject LoadStruct(Loc loc, string type) => LoadStruct(new StructState { location = loc, type = type });


    public GameObject RemoveStruct(int body, int index)
    {
        var g = objects[body].structs[index];
        objects[body].structs[index] = null;
        return g;
    }

    public void AddFromOBJ(string oggPath,string name)
    {
        string ogg = Data.ReadFile(oggPath);
        var si = SMesh.LoadStructInfoOGG(ogg);
        ct.meshTypes.Add(name, si.mesh);
        ct.meshFaces.Add(name, si.faces);
        ct.structTypes.Add(name);
    }




    public Bodies()
    {
        ct.command.Add("struct", (l) =>
        {
            var a1 = l.Load();
            if (a1 == "load")
            {
                var a2 = l.Load();//is the name
                LoadStruct(l.LoadV3(), l.LoadV3(), a2);
            }
        });
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
public struct V3
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

    public static V3 zero = new() {
        x = 0, y = 0, z = 0
    };

    public override string ToString()
    {
        return $"({x},{y},{z})";
    }
}

public struct V3I
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
        return new V3I(this.x + x, this.x + y, this.x + z);
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

    public static bool operator ==(V3I a, V3I b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public static bool operator !=(V3I a, V3I b) => !(a == b);

}

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

    public override string ToString()
    {
        return $"({x},{y},{z},{w})";
    }
}
/// <summary>
/// location to save
/// </summary>
public struct Loc
{
    public V3 position;
    public Quater rotation;

    public Loc(Location loc)
    {
        position = new V3(loc.position);
        rotation = new Quater(loc.rotation);
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
    public void LocateHere(GameObject target)
    {
        var t = target.transform;
        Location l = ToLocation();
        t.SetPositionAndRotation(l.position, l.rotation);
    }

    public static Loc zero = new() { position = V3.zero,rotation=Quater.zero};
    public override string ToString()
    {
        return $"({position}, {rotation})";
    }
}