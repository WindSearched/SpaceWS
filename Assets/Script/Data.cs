using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
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
        catch (System.Exception e)
        {
            Debug.Log("[Data.ReadJson] cannot deseriaize json file:" +
                e.Message);

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
        sw.Write(text);
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
    [JsonIgnore]
    public string dataPath
    {
        get => datasavePath;
        set
        {
            if (directCopyDataWhenChangeDataPath)
                Data.CopyAll(datasavePath, value);
            else
                Data.CopyAll(datasavePath, value);
            datasavePath = value;
        }
    }
    [JsonIgnore]
    public string settingPath => datasavePath + "/setting.json";
    [JsonIgnore]
    public string spacePath => datasavePath + "/spaces/";
    public bool directCopyDataWhenChangeDataPath = true;
}

public class LogicalFace
{
    public Vector3[] vertices;
    public int[] triangles; // 0-based 索引
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
    /// <summary>
    /// load faces by ogg file path
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static LogicalFace[] LoadFacesOGG(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("OBJ 文件不存在: " + path);
            return null;
        }
        return GetFacesOGG(File.ReadAllLines(path));
    }
    /// <summary>
    /// get faces from ogg file lines
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    public static LogicalFace[] GetFacesOGG(string[] lines)
    {
        List<Vector3> vertexList = new List<Vector3>();
        List<LogicalFace> faceList = new List<LogicalFace>();

        foreach (string line in lines)
        {
            if (line.StartsWith("v "))
            {
                string[] parts = line.Split(' ');
                float x = float.Parse(parts[1]);
                float y = float.Parse(parts[2]);
                float z = float.Parse(parts[3]);
                vertexList.Add(new Vector3(x, y, z));
            }
            else if (line.StartsWith("f "))
            {
                string[] parts = line.Substring(2).Split(' ');
                int[] indices = new int[parts.Length];
                Vector3[] faceVerts = new Vector3[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    string s = parts[i].Split('/')[0]; // 取顶点索引
                    int idx = int.Parse(s) - 1; // OBJ 索引从1开始
                    indices[i] = i;             // 三角化用本地索引
                    faceVerts[i] = vertexList[idx];
                }

                // 三角化（如果是四边形或更多顶点）
                List<int> tris = new List<int>();
                for (int i = 1; i < faceVerts.Length - 1; i++)
                {
                    tris.Add(0);
                    tris.Add(i);
                    tris.Add(i + 1);
                }

                faceList.Add(new LogicalFace()
                {
                    vertices = faceVerts,
                    triangles = tris.ToArray()
                });
            }
        }

        return faceList.ToArray();
    }
    /// <summary>
    /// get faces from ogg file text
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static LogicalFace[] GetFacesOGG(string text) => GetFacesOGG(text.Split('\n'));
    /// <summary>
    /// get mesh from logical faces
    /// </summary>
    /// <param name="faces"></param>
    /// <returns></returns>
    public static Mesh GetMesh(LogicalFace[] faces)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        int vertOffset = 0;
        foreach (var face in faces)
        {
            verts.AddRange(face.vertices);
            for (int i = 0; i < face.triangles.Length; i++)
            {
                tris.Add(face.triangles[i] + vertOffset);
            }
            vertOffset += face.vertices.Length;
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
    public static (Mesh mesh, LogicalFace[] faces) LoadStructInfoOGG(string[] lines)
    {
        LogicalFace[] faces = GetFacesOGG(lines);
        Mesh mesh = GetMesh(faces);
        return (mesh, faces);
    }
    public static(Mesh mesh, LogicalFace[] faces) LoadStructInfoOGG(string text) => LoadStructInfoOGG(text.Split('\n'));
    public static (Mesh mesh, LogicalFace[] faces) GetStructInfoOGG(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("OBJ 文件不存在: " + path);
            return default;
        }
        return LoadStructInfoOGG(File.ReadAllLines(path));
    }

    /// <summary>
    /// add mesh to target gameobject
    /// </summary>
    /// <param name="target"></param>
    /// <param name="mesh"></param>
    /// <param name="material"></param>
    public static void AddMesh(GameObject target, Mesh mesh, Material material = null)
    {
        // 添加组件
        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (mf == null) mf = target.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = target.GetComponent<MeshRenderer>();
        if (mr == null) mr = target.AddComponent<MeshRenderer>();
        mr.material = material == null ? ct.defaultMat : material;

        MeshCollider collider = target.GetComponent<MeshCollider>();
        if (collider == null) collider = target.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = false;
    }

    /// <summary>
    /// create a 2d mesh
    /// </summary>
    /// <param name="pts"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static GameObject CreatePolygonMesh(List<Vector3> pts, string name = "")
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
    /// 将 objectA 的逻辑面 faceIndexA 完全对齐到 objectB 的逻辑面 faceIndexB
    /// 法线完全相反，面中心精确对齐
    /// </summary>
    public static bool AlignFaceToFace(
        GameObject objectA,
        LogicalFace[] facesA,
        int faceIndexA,

        GameObject objectB,
        LogicalFace[] facesB,
        int faceIndexB
    )
    {
        //索引合法性检查
        if (objectA == null || objectB == null) return false;
        if (facesA == null || facesB == null) return false;
        if (faceIndexA < 0 || faceIndexA >= facesA.Length) return false;
        if (faceIndexB < 0 || faceIndexB >= facesB.Length) return false;

        LogicalFace faceA = facesA[faceIndexA];
        LogicalFace faceB = facesB[faceIndexB];

        Transform tA = objectA.transform;
        Transform tB = objectB.transform;

        //当前世界面顶点
        Vector3[] wA = GetWorldFaceVertices(tA, faceA);
        Vector3[] wB = GetWorldFaceVertices(tB, faceB);

        //面中心
        Vector3 centerB = GetFaceCenter(wB);

        //面旋转（全部用正法线）
        Quaternion rotA = GetFaceRotation(wA);
        Quaternion rotB = GetFaceRotation(wB);

        //目标旋转：A 的面 → B 面的反方向
        Quaternion targetFaceRot =
            Quaternion.LookRotation(
                 rotB * -Vector3.forward,
                 rotB * Vector3.up
            );

        Quaternion deltaRot = targetFaceRot * Quaternion.Inverse(rotA);

        //旋转物体 A
        tA.rotation = deltaRot * tA.rotation;

        //旋转后重新计算 A 面中心（‼️关键）
        Vector3[] wA2 = GetWorldFaceVertices(tA, faceA);
        Vector3 rotatedCenterA = GetFaceCenter(wA2);

        //平移对齐中心
        Vector3 offset = centerB - rotatedCenterA;
        tA.position += offset;

        return true;
    }

    static Vector3 GetFaceCenter(Vector3[] verts)
    {
        Vector3 sum = Vector3.zero;
        foreach (var v in verts) sum += v;
        return sum / verts.Length;
    }

    static Vector3 GetFaceNormal(Vector3[] verts)
    {
        return Vector3.Normalize(
            Vector3.Cross(verts[1] - verts[0], verts[2] - verts[0])
        );
    }
    static Quaternion GetFaceRotation(Vector3[] verts)
    {
        Vector3 normal = GetFaceNormal(verts);
        Vector3 tangent = Vector3.Normalize(verts[1] - verts[0]);
        Vector3 bitangent = Vector3.Cross(normal, tangent);
        return Quaternion.LookRotation(normal, bitangent);
    }
    static Vector3[] GetWorldFaceVertices(Transform t, LogicalFace face)
    {
        Vector3[] world = new Vector3[face.triangles.Length];
        for (int i = 0; i < face.triangles.Length; i++)
        {
            world[i] = t.TransformPoint(face.vertices[face.triangles[i]]);
        }
        return world;
    }



    /// <summary>
    /// a test obj file to load a cube
    /// </summary>
    public static string cubeOBJ = "# Blender 4.2.1 LTS\r\n# www.blender.org\r\nmtllib testcube.mtl\r\no Cube\r\nv 1.000000 1.000000 -1.000000\r\nv 1.000000 -1.000000 -1.000000\r\nv 1.000000 1.000000 1.000000\r\nv 1.000000 -1.000000 1.000000\r\nv -1.000000 1.000000 -1.000000\r\nv -1.000000 -1.000000 -1.000000\r\nv -1.000000 1.000000 1.000000\r\nv -1.000000 -1.000000 1.000000\r\nvn -0.0000 1.0000 -0.0000\r\nvn -0.0000 -0.0000 1.0000\r\nvn -1.0000 -0.0000 -0.0000\r\nvn -0.0000 -1.0000 -0.0000\r\nvn 1.0000 -0.0000 -0.0000\r\nvn -0.0000 -0.0000 -1.0000\r\nvt 0.625000 0.500000\r\nvt 0.875000 0.500000\r\nvt 0.875000 0.750000\r\nvt 0.625000 0.750000\r\nvt 0.375000 0.750000\r\nvt 0.625000 1.000000\r\nvt 0.375000 1.000000\r\nvt 0.375000 0.000000\r\nvt 0.625000 0.000000\r\nvt 0.625000 0.250000\r\nvt 0.375000 0.250000\r\nvt 0.125000 0.500000\r\nvt 0.375000 0.500000\r\nvt 0.125000 0.750000\r\ns 0\r\nusemtl Material\r\nf 1/1/1 5/2/1 7/3/1 3/4/1\r\nf 4/5/2 3/4/2 7/6/2 8/7/2\r\nf 8/8/3 7/9/3 5/10/3 6/11/3\r\nf 6/12/4 2/13/4 4/5/4 8/14/4\r\nf 2/13/5 1/1/5 3/4/5 4/5/5\r\nf 6/11/6 5/10/6 1/1/6 2/13/6\r\n";
    public static string testStruct1 = "# Blender 4.2.1 LTS\r\n# www.blender.org\r\nmtllib teststruct1.mtl\r\no Cube\r\nv 1.000000 1.000000 -1.000000\r\nv 1.000000 -1.000000 -1.000000\r\nv 1.000000 1.000000 1.000000\r\nv 1.000000 -1.000000 1.000000\r\nv -1.000000 1.000000 -1.000000\r\nv -1.000000 -1.000000 -1.000000\r\nv -1.000000 1.000000 1.000000\r\nv -1.000000 -1.000000 1.000000\r\nv 2.301636 1.000000 -1.000000\r\nv 2.301636 -1.000000 -1.000000\r\nv 2.301636 1.000000 1.000000\r\nv 2.301636 -1.000000 1.000000\r\nv 1.000000 1.000000 -2.137064\r\nv 1.000000 -1.000000 -2.137064\r\nv 2.301636 1.000000 -2.137064\r\nv 2.301636 -1.000000 -2.137064\r\nv 1.000000 4.225416 -1.000000\r\nv 2.301636 4.225416 -1.000000\r\nv 1.000000 4.225416 -2.137064\r\nv 2.301636 4.225416 -2.137064\r\nvn -0.0000 1.0000 -0.0000\r\nvn -0.0000 -0.0000 1.0000\r\nvn -1.0000 -0.0000 -0.0000\r\nvn -0.0000 -1.0000 -0.0000\r\nvn -0.0000 -0.0000 -1.0000\r\nvn 1.0000 -0.0000 -0.0000\r\nvt 0.625000 0.500000\r\nvt 0.875000 0.500000\r\nvt 0.875000 0.750000\r\nvt 0.625000 0.750000\r\nvt 0.375000 0.750000\r\nvt 0.625000 1.000000\r\nvt 0.375000 1.000000\r\nvt 0.375000 0.000000\r\nvt 0.625000 0.000000\r\nvt 0.625000 0.250000\r\nvt 0.375000 0.250000\r\nvt 0.125000 0.500000\r\nvt 0.375000 0.500000\r\nvt 0.125000 0.750000\r\ns 0\r\nusemtl Material\r\nf 1/1/1 5/2/1 7/3/1 3/4/1\r\nf 4/5/2 3/4/2 7/6/2 8/7/2\r\nf 8/8/3 7/9/3 5/10/3 6/11/3\r\nf 6/12/4 2/13/4 4/5/4 8/14/4\r\nf 3/4/2 4/5/2 12/5/2 11/4/2\r\nf 6/11/5 5/10/5 1/1/5 2/13/5\r\nf 10/13/6 9/1/6 11/4/6 12/5/6\r\nf 9/1/6 10/13/6 16/13/6 15/1/6\r\nf 4/5/4 2/13/4 10/13/4 12/5/4\r\nf 1/1/1 3/4/1 11/4/1 9/1/1\r\nf 14/13/5 13/1/5 15/1/5 16/13/5\r\nf 10/13/4 2/13/4 14/13/4 16/13/4\r\nf 2/13/3 1/1/3 13/1/3 14/13/3\r\nf 15/1/5 13/1/5 19/1/5 20/1/5\r\nf 17/1/1 18/1/1 20/1/1 19/1/1\r\nf 1/1/2 9/1/2 18/1/2 17/1/2\r\nf 13/1/3 1/1/3 17/1/3 19/1/3\r\nf 9/1/6 15/1/6 20/1/6 18/1/6\r\n";
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

