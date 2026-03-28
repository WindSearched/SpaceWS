using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SMesh
{

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
			public List<ObjFace> connectors = new();
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
					case "cf":
					{
						data.connectors.Add(data.faces[int.Parse(p[0])]);
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

		public static (GameObject template, GameObject facesTemp, RuntimeFace[] faces, RuntimeFace[] connectors, GameObject connectorsTemplate)
			CreateTemplate(string objPath, string mtlPath, string textureRoot, string name)
		{
			var data = Parse(objPath);
			var fs = Face.BuildRuntimeFaces(data);
			var cfs = Face.BuildConnectorFaces(data);
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

			GameObject cft = new GameObject(name);
			cft.transform.SetParent(ct.structFacesTemplateParent);
			cft.SetActive(false);
			Face.CreateFace(cfs, ms, cft.transform);

			return new(go, ft, fs, cfs, cft);
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
			if (!objectA|| !objectB) return false;
			if (facesA == null || facesB == null) return false;
			if (faceIndexA < 0 || faceIndexA >= facesA.Length) return false;
			if (faceIndexB < 0 || faceIndexB >= facesB.Length) return false;

			RuntimeFace faceA = facesA[faceIndexA];
			RuntimeFace faceB = facesB[faceIndexB];

			Transform tA = objectA.transform;
			Transform tB = objectB.transform;

			Vector3 normalA = tA.TransformDirection(faceA.localNormal).normalized;
			Vector3 normalB = tB.TransformDirection(faceB.localNormal).normalized;

			Vector3[] worldA = TransformVerts(tA, faceA.localVerts);
			Vector3[] worldB = TransformVerts(tB, faceB.localVerts);

			Vector3 centerA = GetFaceCenter(worldA);
			Vector3 centerB = GetFaceCenter(worldB);

			// ---------- 1. 法线对齐 ----------
			Quaternion alignRotation =
				Quaternion.FromToRotation(normalA, -normalB);

			Quaternion newRotation = alignRotation * tA.rotation;
			tA.rotation = newRotation;
			
			// 重新计算
			worldA = TransformVerts(tA, faceA.localVerts);
			normalA = tA.TransformDirection(faceA.localNormal).normalized;

			float[] edgesA = GetEdgeLengths(worldA);
			float[] edgesB = GetEdgeLengths(worldB);

			bool edgeMatch = MatchEdgeSequence(edgesA, edgesB);

			bool perfectFit = false;

			if (edgeMatch)
			{
				perfectFit = TryPerfectFaceAlign(
					tA,
					worldA,
					worldB,
					normalA
				);
			}

			// ---------- 2. 中心对齐 ----------
			Vector3 newCenterA = GetFaceCenter(worldA);
			Vector3 offset = centerB - newCenterA;
			tA.position += offset;

			return perfectFit;
		}
		static bool MatchEdgeSequence(float[] a, float[] b, float tolerance = 0.001f)
		{
			int n = a.Length;

			if (b.Length != n)
				return false;

			// ---------- 正向循环 ----------
			for (int shift = 0; shift < n; shift++)
			{
				bool ok = true;

				for (int i = 0; i < n; i++)
				{
					float ai = a[i];
					float bi = b[(i + shift) % n];

					if (Mathf.Abs(ai - bi) > tolerance)
					{
						ok = false;
						break;
					}
				}

				if (ok)
					return true;
			}

			// ---------- 反向循环 ----------
			for (int shift = 0; shift < n; shift++)
			{
				bool ok = true;

				for (int i = 0; i < n; i++)
				{
					float ai = a[i];
					float bi = b[(shift - i + n) % n];

					if (Mathf.Abs(ai - bi) > tolerance)
					{
						ok = false;
						break;
					}
				}

				if (ok)
					return true;
			}

			return false;
		}
		static float[] GetEdgeLengths(Vector3[] verts)
		{
			int n = verts.Length;
			float[] edges = new float[n];

			for (int i = 0; i < n; i++)
			{
				Vector3 a = verts[i];
				Vector3 b = verts[(i + 1) % n];
				edges[i] = Vector3.Distance(a, b);
			}

			return edges;
		}
		static bool TryPerfectFaceAlign(
			Transform tA,
			Vector3[] worldA,
			Vector3[] worldB,
			Vector3 normal
		)
		{
			if (worldA.Length != worldB.Length)
				return false;

			int n = worldA.Length;

			Vector3 centerA = GetFaceCenter(worldA);
			Vector3 centerB = GetFaceCenter(worldB);

			// 转为相对中心
			Vector3[] a = new Vector3[n];
			Vector3[] b = new Vector3[n];

			for (int i = 0; i < n; i++)
			{
				a[i] = worldA[i] - centerA;
				b[i] = worldB[i] - centerB;
			}

			float tolerance = 0.001f;

			// 尝试所有循环匹配
			for (int shift = 0; shift < n; shift++)
			{
				Quaternion rot = Quaternion.FromToRotation(a[0], b[shift]);

				bool match = true;

				for (int i = 0; i < n; i++)
				{
					Vector3 ra = rot * a[i];
					Vector3 rb = b[(i + shift) % n];

					if ((ra - rb).sqrMagnitude > tolerance * tolerance)
					{
						match = false;
						break;
					}
				}

				if (match)
				{
					tA.rotation = rot * tA.rotation;
					return true;
				}
			}

			return false;
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

		public static RuntimeFace[] BuildConnectorFaces(
			ObjTemp.ObjData obj
		)
		{
			var faces = new RuntimeFace[obj.connectors.Count];

			for (int i = 0; i < obj.connectors.Count; i++)
			{
				var src = obj.connectors[i];
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