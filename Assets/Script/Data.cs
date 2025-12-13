using K4os.Compression.LZ4;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.Rendering.HableCurve;
public static class Data
{   
    /// <summary>
    /// write json data to file
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="path"></param>
    /// <param name="formatting"></param>
    public static void WriteJson<T>(T data, string path, Formatting formatting = Formatting.Indented)
    {

        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new DefaultContractResolver
            {
                // 忽略只读属性（比如 normalized）
                IgnoreSerializableInterface = true
            }
        };
        string json = JsonConvert.SerializeObject(data, formatting, settings);
        string sp = string.Empty;
        using StreamWriter sw = new(sp + path);
        sw.WriteLine(json);
        sw.Close();
    }
    public static T ReadJson<T>(string path)
    {
        if (!FileExists(path))
            return default;

        string json = ReadTextFile(path);

        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch
        {
            Debug.Log("[Data.ReadJson] cannot deseriaize json file");

            return default;
        }
    }
    public static string ReadTextFile(string filePath)
    {
        string result = "";

        // 判断路径是否包含 "://" 或 ":///"，以确定是否在 Android 或网络环境中
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // Android 或 Web 环境，使用 UnityWebRequest 读取文件
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            www.SendWebRequest();

            // 等待请求完成
            while (!www.isDone) { }

            if (www.result == UnityWebRequest.Result.Success)
            {
                result = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError("Error reading file: " + www.error);
            }
        }
        else
        {
            // 其他平台，如 Windows，直接读取文件
            result = File.ReadAllText(filePath);
        }

        return result;
    }
    public static bool FileExists(string path)
    {
        return File.Exists(path.TrimEnd('/'));
    }
    public static void CopyAll(string source, string dest)
    {
        if (!DirectioryExists(source))
            return;
        if (!DirectioryExists(dest))
            DirectoryCreate(dest);

        DirectoryInfo di = new(source);

        foreach (FileInfo fi in di.GetFiles())
        {
            fi.CopyTo(dest + "/" + fi.Name, true);
        }

        foreach (DirectoryInfo d in di.GetDirectories())
        {
            CopyAll(d.FullName, dest + "/" + d.Name);
        }

    }
    public static bool DirectioryExists(string path)
    {
        return Directory.Exists(path.TrimEnd('/'));
    }
    public static void DirectoryCreate(string path)
    {
        Directory.CreateDirectory(path);
    }
    public static void CreateFile(string path, string text, bool rewrite)
    {
        if (FileExists(path) && !rewrite)
            return;
        using StreamWriter sw = new(path);
        var lines = text.Split('\n');
        sw.Write(lines);
        sw.Close();
    }
    public static string ReadFile(string path)
    {
        if (!FileExists(path))
            return string.Empty;
        using StreamReader sr = new(path);
        string result = sr.ReadToEnd();
        sr.Close();
        return result;
    }
}
public class Set
{
    private string datasavePath = Application.persistentDataPath;
    public string dataPath
    {
        get => datasavePath;
        set{
            if(directCopyDataWhenChangeDataPath)
                Data.CopyAll(datasavePath, value);
            else
                Data.CopyAll(datasavePath, value);
            datasavePath = value;
        }
    }
    public string settingPath => datasavePath + "/setting.json";
    public string spacePath => datasavePath + "/spaces/";
    public bool directCopyDataWhenChangeDataPath = true;
}

public static class SMesh
{
    public static Mesh LoadMeshFromOBJ(string objFilePath, Material material = null)
    {
        string fullPath = Path.Combine(Application.dataPath, objFilePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError("OBJ 文件不存在: " + fullPath);
            return null;
        }

        string[] lines = File.ReadAllLines(fullPath);
        return LoadMeshFromOBJ(lines);
    }
    public static Mesh LoadMeshFromTextOBJ(string txt)
    {
        string[] ls = txt.Split('\n');
        return LoadMeshFromOBJ(ls);
    }
    public static Mesh LoadMeshFromOBJ(string[] lines)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        foreach (var line in lines)
        {
            if (line.StartsWith("v "))
            {
                // 顶点
                string[] parts = line.Split(' ');
                float x = float.Parse(parts[1]);
                float y = float.Parse(parts[2]);
                float z = float.Parse(parts[3]);
                vertices.Add(new Vector3(x, y, z));
            }
            else if (line.StartsWith("f "))
            {
                // 面（假设三角形或多边形，做扇形三角化）
                string[] parts = line.Split(' ');
                int[] faceIndices = new int[parts.Length - 1];
                for (int i = 1; i < parts.Length; i++)
                {
                    faceIndices[i - 1] = int.Parse(parts[i].Split('/')[0]) - 1; // 顶点索引从0开始
                }

                // 多边形三角化（扇形法）
                for (int i = 1; i < faceIndices.Length - 1; i++)
                {
                    triangles.Add(faceIndices[0]);
                    triangles.Add(faceIndices[i]);
                    triangles.Add(faceIndices[i + 1]);
                }
            }
        }

        // 创建 Mesh
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();



        return mesh;
    }

    public static void AddMesh(GameObject target, Mesh mesh, Material material = null)
    {
        // 添加组件
        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (mf == null) mf = target.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = target.GetComponent<MeshRenderer>();
        if (mr == null) mr = target.AddComponent<MeshRenderer>();
        mr.material = material != null ? material : new Material(Shader.Find("Standard"));

        MeshCollider collider = target.GetComponent<MeshCollider>();
        if (collider == null) collider = target.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = false;
    }

    public static GameObject CreatePolygonMesh(List<Vector3> pts,string name = "")
    {
        Mesh mesh = new Mesh();

        // --- 1. 将 Vector3 转成 Vector2（用于三角化） ---
        Vector2[] pts2 = new Vector2[pts.Count];
        for (int i = 0; i < pts.Count; i++)
            pts2[i] = new Vector2(pts[i].x, pts[i].z); // 投影到XZ平面，可改Y轴

        // --- 2. 三角化（Ear Clipping） ---
        int[] triangles = Triangulate(pts2);

        // --- 3. 设置 Mesh ---
        mesh.vertices = pts.ToArray();
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject g = new();
        // --- 4. 应用到 MeshFilter ---
        MeshFilter mf = g.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        // --- 5. MeshCollider ---
        MeshCollider collider = g.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;  // 关键！
        collider.convex = false;     // 需要面检测必须 false

        return g;
    }
    static int[] Triangulate(Vector2[] vertices)
    {
        List<int> indices = new List<int>();
        List<int> verts = new List<int>();
        for (int i = 0; i < vertices.Length; i++)
            verts.Add(i);

        int count = verts.Count;
        while (count > 2)
        {
            for (int i = 0; i < count; i++)
            {
                int i0 = verts[(i + 0) % count];
                int i1 = verts[(i + 1) % count];
                int i2 = verts[(i + 2) % count];

                // 添加三角形
                indices.Add(i0);
                indices.Add(i1);
                indices.Add(i2);
                verts.RemoveAt((i + 1) % count);
                break;
            }
            count = verts.Count;
        }
        return indices.ToArray();
    }

    /// <summary>
    /// a test obj file to load a cube
    /// </summary>
    public static string cubeOBJ = "# Blender 4.2.1 LTS\r\n# www.blender.org\r\nmtllib testcube.mtl\r\no Cube\r\nv 1.000000 1.000000 -1.000000\r\nv 1.000000 -1.000000 -1.000000\r\nv 1.000000 1.000000 1.000000\r\nv 1.000000 -1.000000 1.000000\r\nv -1.000000 1.000000 -1.000000\r\nv -1.000000 -1.000000 -1.000000\r\nv -1.000000 1.000000 1.000000\r\nv -1.000000 -1.000000 1.000000\r\nvn -0.0000 1.0000 -0.0000\r\nvn -0.0000 -0.0000 1.0000\r\nvn -1.0000 -0.0000 -0.0000\r\nvn -0.0000 -1.0000 -0.0000\r\nvn 1.0000 -0.0000 -0.0000\r\nvn -0.0000 -0.0000 -1.0000\r\nvt 0.625000 0.500000\r\nvt 0.875000 0.500000\r\nvt 0.875000 0.750000\r\nvt 0.625000 0.750000\r\nvt 0.375000 0.750000\r\nvt 0.625000 1.000000\r\nvt 0.375000 1.000000\r\nvt 0.375000 0.000000\r\nvt 0.625000 0.000000\r\nvt 0.625000 0.250000\r\nvt 0.375000 0.250000\r\nvt 0.125000 0.500000\r\nvt 0.375000 0.500000\r\nvt 0.125000 0.750000\r\ns 0\r\nusemtl Material\r\nf 1/1/1 5/2/1 7/3/1 3/4/1\r\nf 4/5/2 3/4/2 7/6/2 8/7/2\r\nf 8/8/3 7/9/3 5/10/3 6/11/3\r\nf 6/12/4 2/13/4 4/5/4 8/14/4\r\nf 2/13/5 1/1/5 3/4/5 4/5/5\r\nf 6/11/6 5/10/6 1/1/6 2/13/6\r\n";
}

public class StructState
{
    public string type;
    public Loc location;
    public int bodyIndex;

}
public struct Chunk
{
    /// <summary>
    /// position ppf this chunk
    /// </summary>
    public V3I position;
    public List<StructState> structs;

    /// <summary>
    /// get the position of chunk
    /// </summary>
    public Vector3 GetCP() => position.ToVector3Int();


    /// <summary>
    /// note: can used only in unity envirment
    /// </summary>
    /// <returns></returns>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8);

        // 写 V3I position
        bw.Write(position.x);
        bw.Write(position.y);
        bw.Write(position.z);

        // 写 structs 数量
        int count = structs != null ? structs.Count : 0;
        bw.Write(count);

        if (structs != null)
        {
            foreach (var s in structs)
            {
                // 写 string type
                bw.Write(s.type ?? "");

                // 写 Loc
                bw.Write(s.location.position.x);
                bw.Write(s.location.position.y);
                bw.Write(s.location.position.z);

                bw.Write(s.location.rotation.x);
                bw.Write(s.location.rotation.y);
                bw.Write(s.location.rotation.z);
                bw.Write(s.location.rotation.w);

                // 写 bodyIndex
                bw.Write(s.bodyIndex);
            }
        }

        return ms.ToArray();
    }

    // 反序列化 Chunk
    public static Chunk FromBytes(byte[] data)
    {
        Chunk chunk = new Chunk();
        chunk.structs = new List<StructState>();

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms, Encoding.UTF8);

        // 读取 position
        chunk.position.x = br.ReadInt32();
        chunk.position.y = br.ReadInt32();
        chunk.position.z = br.ReadInt32();

        // 读取 structs 数量
        int count = br.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            StructState s = new StructState();

            // 读取 string
            s.type = br.ReadString();

            // 读取 Loc
            s.location.position.x = br.ReadSingle();
            s.location.position.y = br.ReadSingle();
            s.location.position.z = br.ReadSingle();

            s.location.rotation.x = br.ReadSingle();
            s.location.rotation.y = br.ReadSingle();
            s.location.rotation.z = br.ReadSingle();
            s.location.rotation.w = br.ReadSingle();

            // 读取 bodyIndex
            s.bodyIndex = br.ReadInt32();

            chunk.structs.Add(s);
        }

        return chunk;
    }

}

