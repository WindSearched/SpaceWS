using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
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

    // private void OnDestroy()
    // {
    //     env.Dispose();
    // }
}

public class Mod
{
    public class New
    {
        public Loc loc() => new Loc();
        public SMType smType(string type, string mod) => new SMType(type, mod);
        public StructState.Mixture structMixture() => new();
        public StructState structState() => new();
        public StructState structState(SMType type) => new(type);
    }

    public Dictionary<string, LuaTable> mods = new();
    public string path => ct.setting.modPath;
    public void OnStart()
    {
        var env = LuaManager.instance.env;

        void set<Tval>(string key, Tval val) => env.Global.Set(key, val);

        env.Global.Set("Log",(Action<string,string>)ct.log.Write);
        env.Global.Set("Register",(Action<string,LuaTable>)RegistMod);
        env.Global.Set("dLog",(Action<object>)Debug.Log);
        env.Global.Set("Command",(Func<string, bool>)ct.CommandBranch.Command);
        env.Global.Set("swapPage",(Func<string, bool>)ct.pages.Swap);
        env.Global.Set("AddStructOBJ",(Action<string,string>)AddStructFromOBJ);
        env.Global.Set("GetFile",(Func<string,string,string>)ModGetFile);
        env.Global.Set("LoadStruct",(Func<float,float,float,float,float,float,int,string, string,GameObject>)ct.bodies.LoadStruct);
        env.Global.Set("AddChunkGenerator", (Action <ChunkGenerator>) ct.chunkLoader.AddGenerator);
        env.Global.Set("RandomFlt", (Func<int,float,float, float>)ct.curWorldRule.RandFlt);
        env.Global.Set("RandomInt", (Func<int,int,int, int>)ct.curWorldRule.RandInt);
        env.Global.Set("GetNewBodyIndex", (Func<int>)(() => ct.curWorldRule.distribuiteBodyIndex));

        //class
        env.Global.Set("Chunk", typeof(Chunk));
        env.Global.Set("StructState", typeof(StructState));
        env.Global.Set("BodyState", typeof(BodyState));
        env.Global.Set("Loc", typeof(Loc));
        env.Global.Set("Quater", typeof(Quater));
        env.Global.Set("V3I", typeof(V3I));
        env.Global.Set("V3", typeof(V3));
        set("StructMixture", typeof(StructState.Mixture));

        set("new", new New());
        env.Global.Set("LocNew", (Func<float, float, float, float, float, float, Loc>)(
            (x, y, z, rx, ry, rz) => new Loc(x, y, z, rx, ry, rz)));
        env.Global.Set("SMTypeNew", (Func<string,string, SMType>)((type, mod) => new SMType(type, mod)));

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
            string name = Path.GetFileName(p);// mod name
            env.DoString(File.ReadAllText(Path.Combine(p, name +".lua")));
            Load(name, "OnLoad");

            string pp = "";
            string ppp = "";

            pp = p + "/structs";
            if (Data.DirectioryExists(pp))
            {
                foreach (var file in Directory.EnumerateFiles(pp, "*.obj"))
                {
                    var n = Path.GetFileNameWithoutExtension(file);
                    AddStructFromOBJ(name, $"structs/{n}");
                }
            }

            //load data directory
            pp = p + "/data";
            ppp = pp + "/structs";
            if(Data.DirectioryExists(ppp))
            {// if "structs" exists
                foreach (var f in Directory.GetFiles(ppp))
                {
                    if (LoadJson(f, out List<StructData> items))
                    {
                        foreach (var i in items)
                        {
                            i.mod = name;
                            ct.structsInfo.GetAbs(i.smt, () => new StructInfo()).data = i;
                        }
                    }
                    else
                    {
                        string l = $"The json file of {name} in {f} for load struct data is not valid";
                        Debug.Log(l);
                        ct.log.Write("ModLoader", l);
                    }
                }
            }
            ppp = pp + "/items";
            if (Data.DirectioryExists(ppp))
            {
                foreach (var f in Directory.GetFiles(ppp))
                {
                    if (LoadJson(f, out List<ItemData> items))
                    {
                        foreach (var i in items)
                        {
                            i.mod = name;
                            ct.itemsData.Add(i.type, i);
                        }
                    }
                    else
                    {
                        string l = $"The json file of {name} in {f} for load item data is not valid";
                        Debug.Log(l);
                        ct.log.Write("ModLoader", l);
                    }
                }
            }

            ppp = pp + "/structsIcon";
            if (Data.DirectioryExists(ppp))
            {
                foreach (var f in Directory.GetFiles(ppp))
                {
                    if (SMath.Spr.TryLoadFromPNG(f, out Sprite spr, 256))
                    {
                        string fileName =  Data.GetFileName(ppp);
                        ct.structIcons.Add(fileName, spr);
                    }
                }
            }

            ppp = pp + "/materials";
            if (Data.DirectioryExists(ppp))
            {
                foreach (var f in Directory.GetFiles(ppp))
                {
                    if (LoadJson(f, out List<MaterialData> materials))
                    {
                        foreach (var i in materials)
                        {
                            i.mod = name;
                            ct.materials.Set(i.smt, i);
                        }
                    }
                    else
                    {
                        string l = $"The json file of {name} in {f} for load material data is not valid";
                        Debug.Log(l);
                        ct.log.Write("ModLoader", l);
                    }
                }
            }
        }

        bool LoadJson<T>(string path, out List<T> list) where T : new()
        {
            var tx = Data.ReadFile(path);
            var tk = JToken.Parse(tx);
            if (tk.Type == JTokenType.Object)
            {
                list = new List<T>();

                try
                {
                    var d = JsonConvert.DeserializeObject<T>(tx);
                    list.Add(d);
                }
                catch
                {
                    return false;
                }
            }
            else if (tk.Type == JTokenType.Array)
                list = JsonConvert.DeserializeObject<List<T>>(tx);
            else
            {
                list = null;
                return false;
            }
            return true;
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

        if(ct.structTypes.Contains(name))
            return;

        ct.structTypes.Add(name);
        var t = SMesh.ObjTemp.CreateTemplate(p + ".obj", p + ".mtl", Path.GetDirectoryName(p), name);
        var i = ct.structsInfo.GetAbs(SMType.Parse(name), () => new StructInfo());
        i.template = t.template;
        i.faces = t.faces;
        i.facesTamplate = t.facesTemp;
        i.connectorFaceIndexes = t.connectorIndexes;
        i.connectorTamplate = t.connectorsTemplate;

        t.template.SetActive(false);
    }

}