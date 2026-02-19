using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

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
					indices[i] = i; // 三角化用本地索引
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

	public static (Mesh mesh, LogicalFace[] faces) LoadStructInfoOGG(string text) =>
		LoadStructInfoOGG(text.Split('\n'));

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
		collider.sharedMesh = mesh; // 关键！
		collider.convex = false; // 需要面检测必须 false

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

	/// <param name="sourceMesh"></param>
	/// <param name="logicalFaces"></param>
	/// <param name="baseSubMeshIndex">usually is 0</param>
	/// <returns></returns>
	public static Mesh ToSubmesh(
		Mesh sourceMesh,
		List<int[]> logicalFaces,
		out int baseSubMeshIndex
	)
	{
		//复制 Mesh（关键：不破坏原始资源）
		Mesh mesh = Object.Instantiate(sourceMesh);

		int[] allTriangles = mesh.triangles;
		int triangleCount = allTriangles.Length / 3;

		// SubMesh 0：未被逻辑面使用的三角形
		// SubMesh 1..n：每个逻辑面
		mesh.subMeshCount = logicalFaces.Count + 1;
		baseSubMeshIndex = 0;

		// 收集被逻辑面占用的 triangleIndex
		HashSet<int> usedTriangleSet = new HashSet<int>();
		foreach (var face in logicalFaces)
		{
			foreach (int tri in face)
			{
				usedTriangleSet.Add(tri);
			}
		}

		// 构建 SubMesh 0（剩余部分）
		List<int> restTriangles = new List<int>();

		for (int i = 0; i < triangleCount; i++)
		{
			if (!usedTriangleSet.Contains(i))
			{
				restTriangles.Add(allTriangles[i * 3 + 0]);
				restTriangles.Add(allTriangles[i * 3 + 1]);
				restTriangles.Add(allTriangles[i * 3 + 2]);
			}
		}

		mesh.SetTriangles(restTriangles, 0);

		//为每个逻辑面生成一个 SubMesh
		for (int faceIndex = 0; faceIndex < logicalFaces.Count; faceIndex++)
		{
			List<int> faceTriangles = new List<int>();

			foreach (int tri in logicalFaces[faceIndex])
			{
				faceTriangles.Add(allTriangles[tri * 3 + 0]);
				faceTriangles.Add(allTriangles[tri * 3 + 1]);
				faceTriangles.Add(allTriangles[tri * 3 + 2]);
			}

			mesh.SetTriangles(faceTriangles, faceIndex + 1);
		}

		// 可选：重新计算（视情况）
		mesh.RecalculateBounds();
		// mesh.RecalculateNormals(); // 若未修改顶点，一般不需要

		return mesh;
	}


	/// <summary>
	/// a test obj file to load a cube
	/// </summary>
	public static string cubeOBJ =
		"# Blender 4.2.1 LTS\r\n# www.blender.org\r\nmtllib testcube.mtl\r\no Cube\r\nv 1.000000 1.000000 -1.000000\r\nv 1.000000 -1.000000 -1.000000\r\nv 1.000000 1.000000 1.000000\r\nv 1.000000 -1.000000 1.000000\r\nv -1.000000 1.000000 -1.000000\r\nv -1.000000 -1.000000 -1.000000\r\nv -1.000000 1.000000 1.000000\r\nv -1.000000 -1.000000 1.000000\r\nvn -0.0000 1.0000 -0.0000\r\nvn -0.0000 -0.0000 1.0000\r\nvn -1.0000 -0.0000 -0.0000\r\nvn -0.0000 -1.0000 -0.0000\r\nvn 1.0000 -0.0000 -0.0000\r\nvn -0.0000 -0.0000 -1.0000\r\nvt 0.625000 0.500000\r\nvt 0.875000 0.500000\r\nvt 0.875000 0.750000\r\nvt 0.625000 0.750000\r\nvt 0.375000 0.750000\r\nvt 0.625000 1.000000\r\nvt 0.375000 1.000000\r\nvt 0.375000 0.000000\r\nvt 0.625000 0.000000\r\nvt 0.625000 0.250000\r\nvt 0.375000 0.250000\r\nvt 0.125000 0.500000\r\nvt 0.375000 0.500000\r\nvt 0.125000 0.750000\r\ns 0\r\nusemtl Material\r\nf 1/1/1 5/2/1 7/3/1 3/4/1\r\nf 4/5/2 3/4/2 7/6/2 8/7/2\r\nf 8/8/3 7/9/3 5/10/3 6/11/3\r\nf 6/12/4 2/13/4 4/5/4 8/14/4\r\nf 2/13/5 1/1/5 3/4/5 4/5/5\r\nf 6/11/6 5/10/6 1/1/6 2/13/6\r\n";

	public static string testStruct1 =
		"# Blender 4.2.1 LTS\r\n# www.blender.org\r\nmtllib teststruct1.mtl\r\no Cube\r\nv 1.000000 1.000000 -1.000000\r\nv 1.000000 -1.000000 -1.000000\r\nv 1.000000 1.000000 1.000000\r\nv 1.000000 -1.000000 1.000000\r\nv -1.000000 1.000000 -1.000000\r\nv -1.000000 -1.000000 -1.000000\r\nv -1.000000 1.000000 1.000000\r\nv -1.000000 -1.000000 1.000000\r\nv 2.301636 1.000000 -1.000000\r\nv 2.301636 -1.000000 -1.000000\r\nv 2.301636 1.000000 1.000000\r\nv 2.301636 -1.000000 1.000000\r\nv 1.000000 1.000000 -2.137064\r\nv 1.000000 -1.000000 -2.137064\r\nv 2.301636 1.000000 -2.137064\r\nv 2.301636 -1.000000 -2.137064\r\nv 1.000000 4.225416 -1.000000\r\nv 2.301636 4.225416 -1.000000\r\nv 1.000000 4.225416 -2.137064\r\nv 2.301636 4.225416 -2.137064\r\nvn -0.0000 1.0000 -0.0000\r\nvn -0.0000 -0.0000 1.0000\r\nvn -1.0000 -0.0000 -0.0000\r\nvn -0.0000 -1.0000 -0.0000\r\nvn -0.0000 -0.0000 -1.0000\r\nvn 1.0000 -0.0000 -0.0000\r\nvt 0.625000 0.500000\r\nvt 0.875000 0.500000\r\nvt 0.875000 0.750000\r\nvt 0.625000 0.750000\r\nvt 0.375000 0.750000\r\nvt 0.625000 1.000000\r\nvt 0.375000 1.000000\r\nvt 0.375000 0.000000\r\nvt 0.625000 0.000000\r\nvt 0.625000 0.250000\r\nvt 0.375000 0.250000\r\nvt 0.125000 0.500000\r\nvt 0.375000 0.500000\r\nvt 0.125000 0.750000\r\ns 0\r\nusemtl Material\r\nf 1/1/1 5/2/1 7/3/1 3/4/1\r\nf 4/5/2 3/4/2 7/6/2 8/7/2\r\nf 8/8/3 7/9/3 5/10/3 6/11/3\r\nf 6/12/4 2/13/4 4/5/4 8/14/4\r\nf 3/4/2 4/5/2 12/5/2 11/4/2\r\nf 6/11/5 5/10/5 1/1/5 2/13/5\r\nf 10/13/6 9/1/6 11/4/6 12/5/6\r\nf 9/1/6 10/13/6 16/13/6 15/1/6\r\nf 4/5/4 2/13/4 10/13/4 12/5/4\r\nf 1/1/1 3/4/1 11/4/1 9/1/1\r\nf 14/13/5 13/1/5 15/1/5 16/13/5\r\nf 10/13/4 2/13/4 14/13/4 16/13/4\r\nf 2/13/3 1/1/3 13/1/3 14/13/3\r\nf 15/1/5 13/1/5 19/1/5 20/1/5\r\nf 17/1/1 18/1/1 20/1/1 19/1/1\r\nf 1/1/2 9/1/2 18/1/2 17/1/2\r\nf 13/1/3 1/1/3 17/1/3 19/1/3\r\nf 9/1/6 15/1/6 20/1/6 18/1/6\r\n";


	public static class Mtl
	{
		// =========================
		// MTL 解析（最小可用）
		// =========================
		public static Dictionary<string, Material> Load(string path, string texRoot)
		{
			var dict = new Dictionary<string, Material>();
			Material cur = null;

			foreach (var raw in File.ReadAllLines(path))
			{
				var line = raw.Trim();
				if (line.Length == 0 || line.StartsWith("#")) continue;
				var p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				switch (p[0])
				{
					case "newmtl":
						cur = new Material(ct.defaultMat);
						cur.name = p[1];
						dict[p[1]] = cur;
						break;

					case "Kd":
						if (cur != null)
							cur.color = new Color(Parse(p[1]), Parse(p[2]), Parse(p[3]));
						break;

					case "map_Kd":
						if (cur != null)
						{
							string texPath = Path.Combine(texRoot, p[1]);
							if (File.Exists(texPath))
							{
								var data = File.ReadAllBytes(texPath);
								var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
								tex.LoadImage(data);
								cur.mainTexture = tex;
							}
						}

						break;
				}
			}

			return dict;
		}
		static float Parse(string s) => float.Parse(s, CultureInfo.InvariantCulture);
	}

	public static class ObjTemp
	{
		// =========================
		// 数据结构
		// =========================

		public struct FaceVert
		{
			public int v; // vertex index
			public int vt; // uv index
			public int vn; // normal index
		}

		public class ObjFace
		{
			public string material;
			public List<FaceVert> verts; // 原始面（3 / 4 / N）
		}

		public class ObjData
		{
			public List<Vector3> verts = new();
			public List<Vector2> uvs = new();
			public List<Vector3> norms = new();
			public List<ObjFace> faces = new();
		}

		// =========================
		// OBJ 解析（不三角化）
		// =========================

		public static ObjData Parse(string objPath)
		{
			var data = new ObjData();
			string currentMat = "__default";

			foreach (var raw in File.ReadAllLines(objPath))
			{
				var line = raw.Trim();
				if (line.Length == 0 || line.StartsWith("#"))
					continue;

				var p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				switch (p[0])
				{
					case "v":
						data.verts.Add(ParseVec3(p));
						break;

					case "vt":
						data.uvs.Add(new Vector2(ParseFloat(p[1]), ParseFloat(p[2])));
						break;

					case "vn":
						data.norms.Add(ParseVec3(p));
						break;

					case "usemtl":
						currentMat = p[1];
						break;

					case "f":
					{
						var face = new ObjFace
						{
							material = currentMat,
							verts = new List<FaceVert>()
						};

						for (int i = 1; i < p.Length; i++)
							face.verts.Add(ParseFaceIndex(p[i]));

						data.faces.Add(face);
						break;
					}
				}
			}

			return data;
		}

		// =========================
		// 三角化（Triangle Fan）
		// =========================

		public static Mesh BuildMesh(ObjData data, out List<string> subMeshMaterials)
		{
			var outVerts = new List<Vector3>();
			var outUVs = new List<Vector2>();
			var outNorms = new List<Vector3>();

			var trisByMat = new Dictionary<string, List<int>>();
			subMeshMaterials = new List<string>();

			foreach (var face in data.faces)
			{
				if (!trisByMat.TryGetValue(face.material, out var tris))
				{
					tris = new List<int>();
					trisByMat[face.material] = tris;
					subMeshMaterials.Add(face.material);
				}

				if (face.verts.Count < 3)
					continue;

				var v0 = face.verts[0];

				for (int i = 1; i < face.verts.Count - 1; i++)
				{
					AddVert(v0);
					AddVert(face.verts[i]);
					AddVert(face.verts[i + 1]);
				}

				void AddVert(FaceVert fv)
				{
					outVerts.Add(data.verts[fv.v]);
					outUVs.Add(fv.vt >= 0 && fv.vt < data.uvs.Count
						? data.uvs[fv.vt]
						: Vector2.zero);

					outNorms.Add(fv.vn >= 0 && fv.vn < data.norms.Count
						? data.norms[fv.vn]
						: Vector3.up);

					tris.Add(outVerts.Count - 1);
				}
			}

			var mesh = new Mesh();
			mesh.SetVertices(outVerts);
			mesh.SetUVs(0, outUVs);
			mesh.SetNormals(outNorms);

			mesh.subMeshCount = subMeshMaterials.Count;
			for (int i = 0; i < subMeshMaterials.Count; i++)
				mesh.SetTriangles(trisByMat[subMeshMaterials[i]], i);

			mesh.RecalculateBounds();
			return mesh;
		}

		// =========================
		// 工具函数
		// =========================

		static FaceVert ParseFaceIndex(string token)
		{
			var idx = token.Split('/');

			return new FaceVert
			{
				v = int.Parse(idx[0]) - 1,
				vt = idx.Length > 1 && idx[1] != "" ? int.Parse(idx[1]) - 1 : -1,
				vn = idx.Length > 2 && idx[2] != "" ? int.Parse(idx[2]) - 1 : -1
			};
		}

		static float ParseFloat(string s) =>
			float.Parse(s, CultureInfo.InvariantCulture);

		static Vector3 ParseVec3(string[] p) =>
			new Vector3(ParseFloat(p[1]), ParseFloat(p[2]), ParseFloat(p[3]));

		public static (GameObject template, GameObject facesTemp, RuntimeFace[] faces) CreateTemplate(string objPath, string mtlPath, string textureRoot, string name)
		{
			var data = Parse(objPath);
			var fs = Face.BuildRuntimeFaces(data);
			Mesh mesh = BuildMesh(
				data,
				out List<string> subMeshMats
			);

			GameObject go = new GameObject(name);
			go.SetActive(false);
			go.transform.SetParent(ct.templateParent);
			var mr = go.AddComponent<MeshRenderer>();
			var mf = go.AddComponent<MeshFilter>();

			// 添加 MeshCollider（模板也具备碰撞能力）
			var mc = go.AddComponent<MeshCollider>();
			mc.sharedMesh = mesh;
			mc.convex = false; // 若需要 Rigidbody 请改为 true


			Material[] ms = new Material[data.faces.Count];

			Material[] mats = new Material[subMeshMats.Count];
			if (Data.FileExists(mtlPath))
			{
				// 1. 解析 MTL
				Dictionary<string, Material> materialDict = Mtl.Load(mtlPath, textureRoot);
				// 3. 构建材质数组（按 SubMesh 顺序）
				for (int i = 0; i < subMeshMats.Count; i++)
				{
					if (!materialDict.TryGetValue(subMeshMats[i], out mats[i]))
						mats[i] = ct.defaultMat;
				}
				for (int i = 0; i < data.faces.Count; i++)
				{
					ms[i] = materialDict[data.faces[i].material];
				}
			}
			else
			{
				for (int i = 0; i < subMeshMats.Count; i++)
				{
					mats[i] = ct.defaultMat;
				}
				for (int i = 0; i < data.faces.Count; i++)
				{
					ms[i] = ct.defaultMat;
				}
			}
			mr.sharedMaterials = mats;
			mf.sharedMesh = mesh;

			GameObject ft = new GameObject(name);
			ft.transform.SetParent(ct.structFacesTemplateParent);
			ft.SetActive(false);

			Face.CreateFace(fs, ms, ft.transform);

			return new(go, ft, fs);
		}
	}


	public static class Face
	{
		public static bool AlignFaceToFace(
			GameObject objectA,
			RuntimeFace[] facesA,
			int faceIndexA,

			GameObject objectB,
			RuntimeFace[] facesB,
			int faceIndexB
		)
		{
			// ---------- 1. 参数检查 ----------
			if (objectA == null || objectB == null) return false;
			if (facesA == null || facesB == null) return false;
			if (faceIndexA < 0 || faceIndexA >= facesA.Length) return false;
			if (faceIndexB < 0 || faceIndexB >= facesB.Length) return false;

			RuntimeFace faceA = facesA[faceIndexA];
			RuntimeFace faceB = facesB[faceIndexB];

			Transform tA = objectA.transform;
			Transform tB = objectB.transform;

			// ---------- 2. 世界空间面顶点 ----------
			Vector3[] worldA = TransformVerts(tA, faceA.localVerts);
			Vector3[] worldB = TransformVerts(tB, faceB.localVerts);

			// ---------- 3. 世界空间法线 ----------
			Vector3 normalA = tA.TransformDirection(faceA.localNormal).normalized;
			Vector3 normalB = tB.TransformDirection(faceB.localNormal).normalized;

			// ---------- 4. 面中心 ----------
			Vector3 centerA = GetFaceCenter(worldA);
			Vector3 centerB = GetFaceCenter(worldB);

			// ---------- 5. 计算旋转（A 面 → B 面反方向） ----------
			Quaternion alignRotation =
				Quaternion.FromToRotation(normalA, -normalB);

			// ---------- 6. 应用旋转 ----------
			tA.rotation = alignRotation * tA.rotation;

			// ---------- 7. 旋转后重新计算 A 面中心 ----------
			Vector3[] worldA2 = TransformVerts(tA, faceA.localVerts);
			Vector3 newCenterA = GetFaceCenter(worldA2);

			// ---------- 8. 平移对齐 ----------
			Vector3 offset = centerB - newCenterA;
			tA.position += offset;

			return true;
		}
		static Vector3[] TransformVerts(Transform t, Vector3[] localVerts)
		{
			Vector3[] world = new Vector3[localVerts.Length];
			for (int i = 0; i < localVerts.Length; i++)
				world[i] = t.TransformPoint(localVerts[i]);
			return world;
		}

		static Vector3 GetFaceCenter(Vector3[] verts)
		{
			Vector3 sum = Vector3.zero;
			for (int i = 0; i < verts.Length; i++)
				sum += verts[i];
			return sum / verts.Length;
		}
		public static RuntimeFace[] BuildRuntimeFaces(
			ObjTemp.ObjData obj
		)
		{
			var faces = new RuntimeFace[obj.faces.Count];

			for (int i = 0; i < obj.faces.Count; i++)
			{
				var src = obj.faces[i];
				var verts = new Vector3[src.verts.Count];

				for (int v = 0; v < src.verts.Count; v++)
					verts[v] = obj.verts[src.verts[v].v];

				faces[i] = new RuntimeFace
				{
					localVerts = verts,
					localNormal = ComputeFaceNormal(verts)
				};
			}

			return faces;
		}
		static Vector3 ComputeFaceNormal(Vector3[] verts)
		{
			if (verts.Length < 3)
				return Vector3.up;

			Vector3 a = verts[1] - verts[0];
			Vector3 b = verts[2] - verts[0];
			return Vector3.Cross(a, b).normalized;
		}

		public static GameObject CreateFace(
		    RuntimeFace face,
		    string name = "",
		    Material mat = null,
		    Transform parent = null,
		    Transform ownerTransform = null
		)
		{
		    if (face?.localVerts == null || face.localVerts.Length < 3)
		        return null;

		    Mesh mesh = new Mesh();

		    // ---------- 1. 获取本地或世界空间顶点 ----------
		    Vector3[] verts3D = new Vector3[face.localVerts.Length];

		    for (int i = 0; i < face.localVerts.Length; i++)
		    {
		        verts3D[i] = ownerTransform
		            ? ownerTransform.TransformPoint(face.localVerts[i])
		            : face.localVerts[i];
		    }

		    // ---------- 2. 构建面内 2D 坐标系（关键） ----------
		    // 使用面法线生成稳定投影平面
		    Vector3 normal = face.localNormal.normalized;

		    Vector3 axisX = Vector3.Cross(normal, Vector3.up);
		    if (axisX.sqrMagnitude < 1e-6f)
		        axisX = Vector3.Cross(normal, Vector3.right);

		    axisX.Normalize();
		    Vector3 axisY = Vector3.Cross(normal, axisX);

		    // ---------- 3. 投影到 2D ----------
		    Vector2[] verts2D = new Vector2[verts3D.Length];
		    Vector3 origin = verts3D[0];

		    for (int i = 0; i < verts3D.Length; i++)
		    {
		        Vector3 v = verts3D[i] - origin;
		        verts2D[i] = new Vector2(
		            Vector3.Dot(v, axisX),
		            Vector3.Dot(v, axisY)
		        );
		    }

		    // ---------- 4. 三角化（Ear Clipping） ----------
		    int[] triangles = Triangulate(verts2D);
		    if (triangles == null || triangles.Length == 0)
		        return null;

		    // ---------- 5. 构建 Mesh ----------
		    mesh.vertices = verts3D;
		    mesh.triangles = triangles;
		    mesh.RecalculateNormals();
		    mesh.RecalculateBounds();

		    // ---------- 6. 创建 GameObject ----------
		    GameObject g = new GameObject(string.IsNullOrEmpty(name) ? "PolygonFace" : name);
		    g.tag = "structFace";
			if(parent) g.transform.SetParent(parent);

		    MeshFilter mf = g.AddComponent<MeshFilter>();
		    mf.sharedMesh = mesh;

		    MeshRenderer mr = g.AddComponent<MeshRenderer>();
		    mr.sharedMaterial = mat;

		    // ---------- 7. MeshCollider ----------
		    MeshCollider collider = g.AddComponent<MeshCollider>();
		    collider.sharedMesh = mesh;
		    collider.convex = false;

		    return g;
		}

		public static void CreateFace(RuntimeFace[] faces, Material[] mats, Transform parent = null)
		{
			for (int i = 0; i < faces.Length; i++)
			{
				var f =  faces[i];
				CreateFace(f,i.ToString(),mats[i],parent);
			}
		}
	}
	public class RuntimeFace
	{
		// 物体【本地空间】下的面顶点（顺序保持）
		public Vector3[] localVerts;

		// 物体【本地空间】下的面法线（右手系）
		public Vector3 localNormal;
	}
}