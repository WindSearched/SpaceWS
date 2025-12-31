using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XLua;

public class LuaManager : MonoBehaviour
{
    public static LuaManager instance;
    public LuaEnv env { get;private set; }

    void Awake()
    {
        instance = this;
        env = new LuaEnv();
    }

    private void OnDestroy()
    {
        env.Dispose();
    }
}

public class Mod
{
    public Dictionary<string, LuaTable> mods = new();
    public string path => ct.setting.modPath;
    public void OnStart()
    {
        var env = LuaManager.instance.env;
        env.Global.Set("Log",(Action<string,string>)ct.log.Write);
        env.Global.Set("Register",(Action<string,LuaTable>)RegistMod);
        env.Global.Set("dLog",(Action<object>)Debug.Log);
        env.Global.Set("GetFile",(Func<string,string,string>)ModGetFile);
        env.Global.Set("AddStructOGG",(Action<string,string>)AddStructFromOBJ);
        env.Global.Set("LoadStruct",(Func<float,float,float,float,float,float,string,GameObject>)ct.bodies.LoadStruct);

        LoadMod();
    }


    public void OnFinish()
    {
        foreach (var mod in mods.Values)
        {
            mod.Get<Action>("OnExit")?.Invoke();
            mod.Dispose();
        }
    }

    void LoadMod()
    {
        Debug.Log($"Start to load mods, mods path: {path}");
        ct.log.Write("ModLoader",$"Start to load mods, mods path: {path}");

        var env = LuaManager.instance.env;
        if (!Data.DirectioryExists(path))
        {
            Data.DirectoryCreate(path);
        }
        foreach (var p in Directory.GetDirectories(path))
        {
            string name = Path.GetFileName(p);
            env.DoString(File.ReadAllText(Path.Combine(p, name +".lua")));


            Load(name, "OnLoad");
        }
    }

    public void RegistMod(string modName, LuaTable table) => mods.TryAdd(modName, table);

    public void Load(string modName, string funcName)
    {
        if (mods.TryGetValue(modName, out var table))
        {
            table.Get<Action>(funcName)?.Invoke();
        }
    }

    /// <summary>
    /// get file from this mod
    /// </summary>
    /// <param name="modName">mod to get file</param>
    /// <param name="filepath">the relative path</param>
    public string ModGetFile(string modName, string filepath) => Data.ReadFile(ct.mod.path + "/" + modName + "/" + filepath);

    public void AddStructFromOBJ(string mod, string modPath)
    {
        Debug.Log(mod + "/" + Path.GetFileName(modPath));
        ct.bodies.AddFromOGG(ct.mod.path + "/" + mod + "/" + modPath + ".obj",mod + "/" + Path.GetFileName(modPath));
    }
}

public interface SMod
{
    public void OnStart();
    public void OnFinish();
}