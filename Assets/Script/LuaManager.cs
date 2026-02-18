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
        env.Global.Set("Command",(Action<string>)ct.command.Load);
        env.Global.Set("swapPage",(Action<string>)ct.pages.Swap);
        env.Global.Set("AddStructOBJ",(Action<string,string>)AddStructFromOBJ);

        env.Global.Set("GetFile",(Func<string,string,string>)ModGetFile);
        env.Global.Set("LoadStruct",(Func<float,float,float,int,int,int,float,float,float,int,string,GameObject>)ct.bodies.LoadStruct);

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
         string name = mod + "/" + Path.GetFileName(modPath);
         Debug.Log(name);
         string p = ct.mod.path + "/" + mod + "/" + modPath;
        // string pp;
        // // load struct
        // ct.bodies.AddFromOBJ( p + ".obj",name);
        // // load materials
        // pp = p + ".mtl";
        // if (Data.FileExists(pp))
        //     ct.bodies.AddMaterial(SMesh.Mtl.Load(pp, Path.GetDirectoryName(pp)),name);

        ct.structTypes.Add(name);
        var t = SMesh.ObjTemp.CreateTemplate(p + ".obj", p + ".mtl", Path.GetDirectoryName(p), name);
        ct.structTemplate.Add(name, t.template);
        ct.structFaces.Add(name, t.faces);
        ct.structFaceTemplates.Add(name, t.facesTemp);
    }
}

public interface SMod
{
    public void OnStart();
    public void OnFinish();
}