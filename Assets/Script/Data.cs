using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    public string worldPath => datasavePath + "/worlds/";
    public bool directCopyDataWhenChangeDataPath = true;
}