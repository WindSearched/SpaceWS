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
	///
	/// </summary>
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
	public static string cubeOBJ = "# Blender 4.2.1 LTS\r\n# www.blender.org\r\nmtllib testcube.mtl\r\no Cube\r\nv 1.000000 1.000000 -1.000000\r\nv 1.000000 -1.000000 -1.000000\r\nv 1.000000 1.000000 1.000000\r\nv 1.000000 -1.000000 1.000000\r\nv -1.000000 1.000000 -1.000000\r\nv -1.000000 -1.000000 -1.000000\r\nv -1.000000 1.000000 1.000000\r\nv -1.000000 -1.000000 1.000000\r\nvn -0.0000 1.0000 -0.0000\r\nvn -0.0000 -0.0000 1.0000\r\nvn -1.0000 -0.0000 -0.0000\r\nvn -0.0000 -1.0000 -0.0000\r\nvn 1.0000 -0.0000 -0.0000\r\nvn -0.0000 -0.0000 -1.0000\r\nvt 0.625000 0.500000\r\nvt 0.875000 0.500000\r\nvt 0.875000 0.750000\r\nvt 0.625000 0.750000\r\nvt 0.375000 0.750000\r\nvt 0.625000 1.000000\r\nvt 0.375000 1.000000\r\nvt 0.375000 0.000000\r\nvt 0.625000 0.000000\r\nvt 0.625000 0.250000\r\nvt 0.375000 0.250000\r\nvt 0.125000 0.500000\r\nvt 0.375000 0.500000\r\nvt 0.125000 0.750000\r\ns 0\r\nusemtl Material\r\nf 1/1/1 5/2/1 7/3/1 3/4/1\r\nf 4/5/2 3/4/2 7/6/2 8/7/2\r\nf 8/8/3 7/9/3 5/10/3 6/11/3\r\nf 6/12/4 2/13/4 4/5/4 8/14/4\r\nf 2/13/5 1/1/5 3/4/5 4/5/5\r\nf 6/11/6 5/10/6 1/1/6 2/13/6\r\n";
	public static string testStruct1 = "# Blender 4.2.1 LTS\r\n# www.blender.org\r\nmtllib teststruct1.mtl\r\no Cube\r\nv 1.000000 1.000000 -1.000000\r\nv 1.000000 -1.000000 -1.000000\r\nv 1.000000 1.000000 1.000000\r\nv 1.000000 -1.000000 1.000000\r\nv -1.000000 1.000000 -1.000000\r\nv -1.000000 -1.000000 -1.000000\r\nv -1.000000 1.000000 1.000000\r\nv -1.000000 -1.000000 1.000000\r\nv 2.301636 1.000000 -1.000000\r\nv 2.301636 -1.000000 -1.000000\r\nv 2.301636 1.000000 1.000000\r\nv 2.301636 -1.000000 1.000000\r\nv 1.000000 1.000000 -2.137064\r\nv 1.000000 -1.000000 -2.137064\r\nv 2.301636 1.000000 -2.137064\r\nv 2.301636 -1.000000 -2.137064\r\nv 1.000000 4.225416 -1.000000\r\nv 2.301636 4.225416 -1.000000\r\nv 1.000000 4.225416 -2.137064\r\nv 2.301636 4.225416 -2.137064\r\nvn -0.0000 1.0000 -0.0000\r\nvn -0.0000 -0.0000 1.0000\r\nvn -1.0000 -0.0000 -0.0000\r\nvn -0.0000 -1.0000 -0.0000\r\nvn -0.0000 -0.0000 -1.0000\r\nvn 1.0000 -0.0000 -0.0000\r\nvt 0.625000 0.500000\r\nvt 0.875000 0.500000\r\nvt 0.875000 0.750000\r\nvt 0.625000 0.750000\r\nvt 0.375000 0.750000\r\nvt 0.625000 1.000000\r\nvt 0.375000 1.000000\r\nvt 0.375000 0.000000\r\nvt 0.625000 0.000000\r\nvt 0.625000 0.250000\r\nvt 0.375000 0.250000\r\nvt 0.125000 0.500000\r\nvt 0.375000 0.500000\r\nvt 0.125000 0.750000\r\ns 0\r\nusemtl Material\r\nf 1/1/1 5/2/1 7/3/1 3/4/1\r\nf 4/5/2 3/4/2 7/6/2 8/7/2\r\nf 8/8/3 7/9/3 5/10/3 6/11/3\r\nf 6/12/4 2/13/4 4/5/4 8/14/4\r\nf 3/4/2 4/5/2 12/5/2 11/4/2\r\nf 6/11/5 5/10/5 1/1/5 2/13/5\r\nf 10/13/6 9/1/6 11/4/6 12/5/6\r\nf 9/1/6 10/13/6 16/13/6 15/1/6\r\nf 4/5/4 2/13/4 10/13/4 12/5/4\r\nf 1/1/1 3/4/1 11/4/1 9/1/1\r\nf 14/13/5 13/1/5 15/1/5 16/13/5\r\nf 10/13/4 2/13/4 14/13/4 16/13/4\r\nf 2/13/3 1/1/3 13/1/3 14/13/3\r\nf 15/1/5 13/1/5 19/1/5 20/1/5\r\nf 17/1/1 18/1/1 20/1/1 19/1/1\r\nf 1/1/2 9/1/2 18/1/2 17/1/2\r\nf 13/1/3 1/1/3 17/1/3 19/1/3\r\nf 9/1/6 15/1/6 20/1/6 18/1/6\r\n";


	public static class LogicalFaceVoxelizer
	{
		public static List<VoxelBox> GenerateFilledVoxels(
			LogicalFace[] logicalFaces,
			Transform parent,
			float voxelSize
		)
		{
			List<VoxelBox> result = new List<VoxelBox>();

			Bounds bounds = CalculateBounds(logicalFaces);

			int xCount = Mathf.CeilToInt(bounds.size.x / voxelSize);
			int yCount = Mathf.CeilToInt(bounds.size.y / voxelSize);
			int zCount = Mathf.CeilToInt(bounds.size.z / voxelSize);

			Vector3 min = bounds.min;

			for (int x = 0; x < xCount; x++)
			for (int y = 0; y < yCount; y++)
			for (int z = 0; z < zCount; z++)
			{
				Vector3 centerLocal = min + new Vector3(
					(x + 0.5f) * voxelSize,
					(y + 0.5f) * voxelSize,
					(z + 0.5f) * voxelSize
				);

				Bounds voxelBounds = new Bounds(centerLocal, Vector3.one * voxelSize);

				List<int> hitFaces = GetIntersectedLogicalFaces(
					voxelBounds,
					logicalFaces
				);

				bool inside = hitFaces.Count > 0 || PointInsideSolid(centerLocal, logicalFaces);

				if (!inside)
					continue;

				// === 创建 BoxCollider ===
				GameObject go = new GameObject($"Voxel_{x}_{y}_{z}");
				go.transform.parent = parent;
				go.transform.localPosition = centerLocal;
				go.transform.localScale = Vector3.one * voxelSize;

				BoxCollider bc = go.AddComponent<BoxCollider>();
				bc.size = Vector3.one;

				result.Add(new VoxelBox
				{
					collider = bc,
					logicalFaceIds = hitFaces
				});
			}

			return result;
		}

		// ------------------ 工具方法 ------------------

		private static Bounds CalculateBounds(LogicalFace[] faces)
		{
			Bounds b = new Bounds(faces[0].vertices[0], Vector3.zero);
			foreach (var f in faces)
				foreach (var v in f.vertices)
					b.Encapsulate(v);
			return b;
		}

		private static List<int> GetIntersectedLogicalFaces(
			Bounds voxel,
			LogicalFace[] faces
		)
		{
			List<int> result = new List<int>();

			for (int i = 0; i < faces.Length; i++)
			{
				if (LogicalFaceIntersectsBox(faces[i], voxel))
					result.Add(i);
			}

			return result;
		}

		private static bool LogicalFaceIntersectsBox(LogicalFace face, Bounds box)
		{
			var verts = face.vertices;
			var tris = face.triangles;

			for (int i = 0; i < tris.Length; i += 3)
			{
				Vector3 v0 = verts[tris[i]];
				Vector3 v1 = verts[tris[i + 1]];
				Vector3 v2 = verts[tris[i + 2]];

				Vector3 center = (v0 + v1 + v2) / 3f;

				if (box.Contains(v0) || box.Contains(v1) || box.Contains(v2) || box.Contains(center))
					return true;
			}

			return false;
		}

		// 射线法判断体素中心是否在封闭体内
		private static bool PointInsideSolid(Vector3 point, LogicalFace[] faces)
		{
			int hitCount = 0;
			Vector3 dir = Vector3.right;

			foreach (var face in faces)
			{
				var v = face.vertices;
				var t = face.triangles;

				for (int i = 0; i < t.Length; i += 3)
				{
					if (RayIntersectsTriangle(point, dir, v[t[i]], v[t[i + 1]], v[t[i + 2]]))
						hitCount++;
				}
			}

			return (hitCount & 1) == 1;
		}

		private static bool RayIntersectsTriangle(
			Vector3 origin,
			Vector3 dir,
			Vector3 v0,
			Vector3 v1,
			Vector3 v2
		)
		{
			Vector3 e1 = v1 - v0;
			Vector3 e2 = v2 - v0;
			Vector3 h = Vector3.Cross(dir, e2);
			float a = Vector3.Dot(e1, h);
			if (Mathf.Abs(a) < 1e-6f) return false;

			float f = 1f / a;
			Vector3 s = origin - v0;
			float u = f * Vector3.Dot(s, h);
			if (u < 0 || u > 1) return false;

			Vector3 q = Vector3.Cross(s, e1);
			float v = f * Vector3.Dot(dir, q);
			if (v < 0 || u + v > 1) return false;

			float t = f * Vector3.Dot(e2, q);
			return t > 1e-6f;
		}
	}
	/// <summary>
	/// 简单的 MTL 文件解析器，将 MTL 转换为 Unity 可用的 Material
	/// 支持常见字段：newmtl, Ka, Kd, Ks, Ns, d/Tr, map_Kd, map_Bump/map_bump
	/// </summary>
	public static class Mtl
	{
	    /// <summary>
	    /// 从 mtl 文件生成材质字典
	    /// </summary>
	    /// <param name="mtlPath">.mtl 文件路径（绝对或相对）</param>
	    /// <param name="textureRoot">纹理查找根目录（通常是 mtl 文件所在目录）</param>
	    public static Dictionary<string, Material> Load(string mtlPath, string textureRoot)
	    {
	        var materials = new Dictionary<string, Material>();

	        if (!File.Exists(mtlPath))
	        {
	            Debug.LogError("MTL file not found: " + mtlPath);
	            return materials;
	        }

	        Material currentMat = null;
	        string currentName = null;

	        foreach (var rawLine in File.ReadAllLines(mtlPath))
	        {
	            var line = rawLine.Trim();
	            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
	                continue;

	            var parts = SplitLine(line);
	            if (parts.Count == 0) continue;

	            switch (parts[0])
	            {
	                case "newmtl":
	                    currentName = parts[1];
	                    currentMat = CreateDefaultMaterial(currentName);
	                    materials[currentName] = currentMat;
	                    break;

	                case "Ka": // 环境色（Unity 中通常忽略或弱化）
	                    // 可根据需要处理
	                    break;

	                case "Kd": // 漫反射颜色
	                    if (currentMat != null)
	                        currentMat.color = ParseColor(parts);
	                    break;

	                case "Ks": // 高光颜色
	                    if (currentMat != null && currentMat.HasProperty("_SpecColor"))
	                        currentMat.SetColor("_SpecColor", ParseColor(parts));
	                    break;

	                case "Ns": // 高光强度
	                    if (currentMat != null && currentMat.HasProperty("_Glossiness"))
	                        currentMat.SetFloat("_Glossiness", ParseFloat(parts[1]) / 1000f);
	                    break;

	                case "d": // 透明度
	                    if (currentMat != null)
	                        SetTransparency(currentMat, ParseFloat(parts[1]));
	                    break;

	                case "Tr": // 透明度（反向）
	                    if (currentMat != null)
	                        SetTransparency(currentMat, 1f - ParseFloat(parts[1]));
	                    break;

	                case "map_Kd": // 漫反射贴图
	                    if (currentMat != null)
	                    {
	                        var tex = LoadTexture(parts[1], textureRoot);
	                        if (tex != null)
	                            currentMat.mainTexture = tex;
	                    }
	                    break;

	                case "map_Bump":
	                case "map_bump": // 法线贴图
	                    if (currentMat != null)
	                    {
	                        var bump = LoadTexture(parts[1], textureRoot);
	                        if (bump != null && currentMat.HasProperty("_BumpMap"))
	                        {
	                            currentMat.EnableKeyword("_NORMALMAP");
	                            currentMat.SetTexture("_BumpMap", bump);
	                        }
	                    }
	                    break;
	            }
	        }

	        return materials;
	    }

	    private static Material CreateDefaultMaterial(string name)
	    {
	        // 使用 URP/Lit 或 Standard，根据项目情况修改
	        Shader shader = Shader.Find("Standard");
	        var mat = new Material(shader)
	        {
	            name = name
	        };
	        mat.color = Color.white;
	        return mat;
	    }

	    private static List<string> SplitLine(string line)
	    {
	        return new List<string>(line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
	    }

	    private static Color ParseColor(List<string> parts)
	    {
	        float r = ParseFloat(parts[1]);
	        float g = ParseFloat(parts[2]);
	        float b = ParseFloat(parts[3]);
	        return new Color(r, g, b, 1f);
	    }

	    private static float ParseFloat(string s)
	    {
	        return float.Parse(s, CultureInfo.InvariantCulture);
	    }

	    private static void SetTransparency(Material mat, float alpha)
	    {
	        alpha = Mathf.Clamp01(alpha);
	        var color = mat.color;
	        color.a = alpha;
	        mat.color = color;

	        if (alpha < 0.999f)
	        {
	            mat.SetFloat("_Mode", 3);
	            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
	            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
	            mat.SetInt("_ZWrite", 0);
	            mat.DisableKeyword("_ALPHATEST_ON");
	            mat.EnableKeyword("_ALPHABLEND_ON");
	            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
	            mat.renderQueue = 3000;
	        }
	    }

	    private static Texture2D LoadTexture(string texName, string root)
	    {
	        var path = Path.Combine(root, texName);
	        if (!File.Exists(path))
	        {
	            Debug.LogWarning("Texture not found: " + path);
	            return null;
	        }

	        var data = File.ReadAllBytes(path);
	        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
	        tex.LoadImage(data);
	        tex.name = Path.GetFileNameWithoutExtension(texName);
	        return tex;
	    }
	}


	/// <summary>
	/// 纯运行时 OBJ + MTL 加载器
	/// - 支持 usemtl / 多 SubMesh
	/// - 支持三角面 / 四边面 / N 边面（扇形三角化）
	/// - 不依赖 Editor / AssetDatabase
	/// - 生成 SetActive(false) 的运行时“物体模板”，可 Instantiate 复用
	/// </summary>
	public static class ObjTemplate
	{
	    // =========================
	    // 对外入口
	    // =========================
	    public static GameObject CreateTemplate(string objPath, string mtlPath, string textureRoot)
	    {
	        // 1. 解析 MTL
	        Dictionary<string, Material> materialDict = LoadMtl(mtlPath, textureRoot);

	        // 2. 解析 OBJ
	        BuildMeshFromObj(objPath, out Mesh mesh, out List<string> subMeshMatNames);

	        // 3. 构建材质数组（按 SubMesh 顺序）
	        Material[] mats = new Material[subMeshMatNames.Count];
	        for (int i = 0; i < subMeshMatNames.Count; i++)
	        {
	            if (!materialDict.TryGetValue(subMeshMatNames[i], out mats[i]))
	                mats[i] = new Material(Shader.Find("Standard"));
	        }

	        // 4. 创建运行时模板 GameObject
	        GameObject go = new GameObject("__RT_Template_" + Path.GetFileNameWithoutExtension(objPath));
	        var mf = go.AddComponent<MeshFilter>();
	        var mr = go.AddComponent<MeshRenderer>();

	        mf.sharedMesh = mesh;
	        mr.sharedMaterials = mats;

	        go.SetActive(false);
	        AttachRuntimeRoot(go);
	        return go;
	    }

	    // =========================
	    // OBJ 解析（完整、安全）
	    // =========================
	    static void BuildMeshFromObj(string path, out Mesh mesh, out List<string> subMeshMatNames)
	    {
	        var verts = new List<Vector3>();
	        var uvs   = new List<Vector2>();
	        var norms = new List<Vector3>();

	        var finalVerts = new List<Vector3>();
	        var finalUVs   = new List<Vector2>();
	        var finalNorms = new List<Vector3>();

	        var trisByMat = new Dictionary<string, List<int>>();
	        subMeshMatNames = new List<string>();

	        string currentMat = "__default";
	        trisByMat[currentMat] = new List<int>();
	        subMeshMatNames.Add(currentMat);

	        // 局部结构与函数（作用域正确）
	        FaceVert ParseFace(string token)
	        {
	            var idx = token.Split('/');
	            int v  = int.Parse(idx[0]) - 1;
	            int vt = idx.Length > 1 && idx[1] != "" ? int.Parse(idx[1]) - 1 : -1;
	            int vn = idx.Length > 2 && idx[2] != "" ? int.Parse(idx[2]) - 1 : -1;

	            return new FaceVert
	            {
	                v  = verts[v],
	                uv = vt >= 0 && vt < uvs.Count ? uvs[vt] : Vector2.zero,
	                n  = vn >= 0 && vn < norms.Count ? norms[vn] : Vector3.up
	            };
	        }

	        foreach (var raw in File.ReadAllLines(path))
	        {
	            var line = raw.Trim();
	            if (line.Length == 0 || line.StartsWith("#")) continue;

	            var p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

	            switch (p[0])
	            {
	                case "v":
	                    verts.Add(ParseVec3(p));
	                    break;

	                case "vt":
	                    uvs.Add(new Vector2(Parse(p[1]), Parse(p[2])));
	                    break;

	                case "vn":
	                    norms.Add(ParseVec3(p));
	                    break;

	                case "usemtl":
	                    currentMat = p[1];
	                    if (!trisByMat.ContainsKey(currentMat))
	                    {
	                        trisByMat[currentMat] = new List<int>();
	                        subMeshMatNames.Add(currentMat);
	                    }
	                    break;

	                case "f":
	                    int faceCount = p.Length - 1;
	                    if (faceCount < 3) break;

	                    var v0 = ParseFace(p[1]);
	                    for (int i = 2; i < faceCount; i++)
	                    {
	                        var v1 = ParseFace(p[i]);
	                        var v2 = ParseFace(p[i + 1]);

	                        finalVerts.Add(v0.v);
	                        finalUVs.Add(v0.uv);
	                        finalNorms.Add(v0.n);
	                        trisByMat[currentMat].Add(finalVerts.Count - 1);

	                        finalVerts.Add(v1.v);
	                        finalUVs.Add(v1.uv);
	                        finalNorms.Add(v1.n);
	                        trisByMat[currentMat].Add(finalVerts.Count - 1);

	                        finalVerts.Add(v2.v);
	                        finalUVs.Add(v2.uv);
	                        finalNorms.Add(v2.n);
	                        trisByMat[currentMat].Add(finalVerts.Count - 1);
	                    }
	                    break;
	            }
	        }

	        mesh = new Mesh();
	        mesh.SetVertices(finalVerts);
	        mesh.SetUVs(0, finalUVs);
	        mesh.SetNormals(finalNorms);

	        mesh.subMeshCount = subMeshMatNames.Count;
	        for (int i = 0; i < subMeshMatNames.Count; i++)
	            mesh.SetTriangles(trisByMat[subMeshMatNames[i]], i);

	        mesh.RecalculateBounds();
	    }

	    // =========================
	    // MTL 解析（最小可用）
	    // =========================
	    static Dictionary<string, Material> LoadMtl(string path, string texRoot)
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
	                    cur = new Material(Shader.Find("Standard"));
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

	    // =========================
	    // 工具
	    // =========================
	    struct FaceVert
	    {
	        public Vector3 v;
	        public Vector2 uv;
	        public Vector3 n;
	    }

	    static float Parse(string s) => float.Parse(s, CultureInfo.InvariantCulture);

	    static Vector3 ParseVec3(string[] p) =>
	        new Vector3(Parse(p[1]), Parse(p[2]), Parse(p[3]));

	    static Transform _root;
	    static void AttachRuntimeRoot(GameObject go)
	    {
	        if (_root == null)
	        {
	            var r = new GameObject("__RuntimeTemplates__");
	            UnityEngine.Object.DontDestroyOnLoad(r);
	            _root = r.transform;
	        }
	        go.transform.SetParent(_root);
	    }
	}




	public class VoxelBox
	{
		public BoxCollider collider;
		public List<int> logicalFaceIds;
	}
}