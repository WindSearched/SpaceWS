using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MemoryPack;
using UnityEngine;
using UnityEngine.Networking;
using XLua;

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
		using StreamWriter sw = new(path);
		sw.WriteLine(json);
		sw.Close();

	}
	public static T ReadJson<T>(string path) where T : new()
	{
		if (!FileExists(path))
			return new T();
		return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
	}
	public static T ReadJsonFromText<T>(string textPath) where T : new()
	{
		string json = ReadTextFile(textPath);

		try
		{
			return JsonConvert.DeserializeObject<T>(json);
		}
		catch (Exception e)
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
	public static string GetFileName(string path) => Path.GetFileName(path);

}
public class Set
{
	private string datasavePath = Application.persistentDataPath;
	public string modPath = Application.persistentDataPath + "/mod/";
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
	[JsonIgnore]
	public string exSpacePath => spacePath + exSpaceName;
	[JsonIgnore]
	public string exRulePath => exSpacePath + "/rule.json";
	[JsonIgnore]
	public string logPath => datasavePath + "/logs/";

	public bool directCopyDataWhenChangeDataPath = true;
	public bool toLog = true;

	public string exSpaceName = "main";
	public string defaultName = "wsspace";
	public int objectpPoolSize = 8192;
	/// <summary>
	/// Amount to load when chunkloader load per tick
	/// </summary>
	public int loadobjectsPerTick = 64;
	/// <summary>
	/// if it is null, do not log
	/// </summary>
	public bool canLog = true;

	public int chunkUnit = 32;
	/// <summary>
	/// region is composited from 16*16 chunk
	/// </summary>
	public int regionSize = 16;

	public Scolor outlineColor = Scolor.outline;
	public int outlineColorIntensity = 25;
	public float outlineWidth = 0.0015f;
	public int debugLineNumber = 16;
	public bool debugShowIndex = true;
	public int tickPerSecond = 20;
}

public class LogicalFace
{
	public Vector3[] vertices;
	public int[] triangles; // 0-based 索引
}

[MemoryPackable][LuaCallCSharp]
public partial class Chunk
{
	[MemoryPackConstructor]
	public Chunk(){}
	public Chunk(V3I cp)
	{
		position = cp;
	}
	/// <summary>
	/// position ppf this chunk
	/// </summary>
	public V3I position;
	public List<StructState> structs = new();
	public List<BodyState> bodies = new();

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

		bw.Write(bodies.Count);
		for (int i = 0; i < bodies.Count; i++)
		{
			bw.Write(bodies[i]);
		}
		// 写 structs 数量
		int count = structs?.Count ?? 0;
		bw.Write(count);

		if (structs != null)
		{
			foreach (var s in structs)
			{
				bw.Write(s);
			}
		}

		return ms.ToArray();
	}

	// 反序列化 Chunk
	public static Chunk FromBytes(byte[] data)
	{
		if (data == null)
		{
			return null;
		}

		using var ms = new MemoryStream(data);
		using var br = new BinaryReader(ms, Encoding.UTF8);

		// 读取 position
		V3I pos;
		pos.x = br.ReadInt32();
		pos.y = br.ReadInt32();
		pos.z = br.ReadInt32();

		Chunk chunk = new Chunk(pos);
		chunk.structs = new List<StructState>();

		int c = br.ReadInt32();
		for (int i = 0; i < c; i++)
		{
			chunk.bodies.Add(BodyState.FromBytes(br));
		}

		// 读取 structs 数量
		int count = br.ReadInt32();

		for (int i = 0; i < count; i++)
		{
			StructState s = StructState.FromBytes(br);
			chunk.structs.Add(s);
		}

		return chunk;
	}
}

/// <summary>
/// setting for single world
/// </summary>
[Serializable]
public class Rule
{
	public int chunk_unit = 32;
	public string name = "space";
	public string seed = "";
	/// <summary>
	/// chunck radius to load
	/// </summary>
	public int loadRadius = 4;

	public void SetJson(string path) =>Data.WriteJson(this, path);

	public int _seedInt = -114514;
	public int seedInt
	{
		get
		{
			if (_seedInt == -114514) _seedInt = seed.GetHashCode() + 6;
			return _seedInt;
		}
	}

	public bool chunkload = true;//





	public int newerBodyIndex = 0;
	[JsonIgnore]
	public int distribuiteBodyIndex => newerBodyIndex++;

	public int RandInt(int seed, int max, int min) => SMath.Random(seed.GetHashCode() + seedInt, max, min);
	public float RandFlt (int seed, float max, float min) => SMath.Random(seed.GetHashCode() - seedInt, max, min);
}
[Serializable]
public struct Scolor
{
	public float r, g, b;
	public Color ToColor () => new Color(r, g, b);

	public Scolor (float r, float g, float b) => this.r = this.r = this.g = this.b = b;

	public static Scolor outline = new(){r = 1, g = 0.18f, b = 0};

}