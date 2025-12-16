using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class Body
{
    public int index;
    public Loc location;
    public List<V3> points;

}

public class Element
{
    public string type = "";
    public int bodyIndex;
    public bool isStruct;

}
public class Struct :Element
{
    public Loc location;
}

[Serializable]
public class StructData
{
    public string meshType;

}
public class Face :Element
{
    public int points;
    public List<int> pointIndexs;
}

public class Bodies
{
    public struct body
    {
        public List<Struct> structs;
        public List<Face> faces;
    }
    public struct obj
    {
        public GameObject self;
        public List<GameObject> structs;
        public List<GameObject> faces;
    }
    public Dictionary<int,body> datas = new();
    public Dictionary<int, obj> objects = new();

    public void LoadVoidBody(int index)
    {
        datas.Add(index, new body()
        {
            structs = new(),
            faces = new()
        });
        objects.Add(index, new obj()
        {
            self = GameObject.Instantiate(ct.defualtBody,ct.bodiesParent),
            structs = new(),
            faces = new()
        });
    }
    public GameObject LoadStruct(Struct strct, Material material = null)
    {
        if (!datas.ContainsKey(strct.bodyIndex))
        {
            LoadVoidBody(strct.bodyIndex);
        }
        datas[strct.bodyIndex].structs.Add(strct);

        var g = new GameObject(datas[strct.bodyIndex].structs.Count.ToString());
        SMesh.AddMesh(g, ct.meshTypes[strct.type]);//it is test just
        g.transform.SetParent(objects[strct.bodyIndex].self.transform);
        objects[strct.bodyIndex].structs.Add(g);

        strct.location.LocateHere(g);
        return g;
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
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public static V3 zero = new() {
        x = 0, y = 0, z = 0
    };
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

    public static V3I zero = new() {
        x = 0, y = 0, z = 0
    };

    public override string ToString() => $"({x},{y},{z})";
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
    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }

    public static Quater zero = new()
    {
        x = 0, y = 0,z = 0,w = 0
    };
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
}