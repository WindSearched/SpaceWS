using UnityEngine;
using UnityEngine.InputSystem;

public class CenterSystem : MonoBehaviour
{
    public static string fp;
    public static InputAction move;
    public static InputAction mouseD;
    private void Start()
    {
        ct.log.Write("Center","Starts to load the center");
        ct.mousecast = new();

        move = ct.action.Add("move",InputActionType.Value);
        ct.action.AddVector2(move);
        mouseD = ct.action.Add("mouseDelta",InputActionType.Value);
        ct.action.AddBiding(mouseD,SAction.keyTable["mouseDelta"]);

        //load setting data
        fp = Application.persistentDataPath + "/setpath";
        if (!Data.FileExists(fp))
        {
            ct.setting = new();
            Data.CreateFile(fp,ct.setting.settingPath.ToString(),false);
        }
        else
        {
            var p = Data.ReadFile(fp);
            ct.setting = Data.ReadJson<Set>(p);
        }
        //

        ct.curWorldRule.JsonGet(ct.setting.exRulePath);

        Tick.Reg(new()//update position every tick
        {
            offset = 1,
            onTick = (TickReg reg) =>
            {
                ct.UpdatePerTick();

                var p = move.ReadValue<Vector2>();// player move diretion
                ct.playerCanMove = p != Vector2.zero;
                ct.wasdDirection = p;

                ct.fps = 1f / Time.unscaledDeltaTime;

                Tick.Reg(reg);
            }
        });

        ct.chunkLoader = new(ct.bodies);
        ct.chunkLoader.generators.Add(chunk =>
        {
            var cp = chunk.position;
            var cunit = ct.curWorldRule.chunk_unit;

            V3 p = new(
                ct.curWorldRule.RandFlt(cp.x + cp.y - cp.z,cunit, 0),
                ct.curWorldRule.RandFlt(cp.x - cp.y - cp.z,cunit, 0),
                ct.curWorldRule.RandFlt(cp.x + cp.y + cp.z,cunit, 0)
                );
            V3 r = new(
                ct.curWorldRule.RandFlt(cp.x - cp.y + cp.z,cunit, 0),
                ct.curWorldRule.RandFlt(cp.x + cp.y + cp.z,cunit, 0),
                ct.curWorldRule.RandFlt(cp.x - cp.y + cp.z,cunit, 0)
            );

            int id = ct.curWorldRule.distribuiteBodyIndex;
            chunk.bodies.Add(new()
            {
                index = id,
                location = new(p,r)
            });
            chunk.structs.Add(new()
            {
                isStruct =  true,
                location = Loc.zero,
                bodyIndex = ct.curWorldRule.distribuiteBodyIndex,
                type = ct.structTypes[ct.curWorldRule.RandInt(cp.x - cp.y + cp.z, ct.structTypes.Count, 0)]
            });
        });

        ct.defualtBody = Resources.Load("Body") as GameObject;
        ct.bodiesParent = GameObject.Find("Bodies").transform;
        ct.defaultMat = Resources.Load("DefaultMat") as Material;

        var si = SMesh.LoadStructInfoOGG(SMesh.cubeOBJ);
        ct.meshTypes.Add("test/normalCube", si.mesh);
        ct.meshFaces.Add("test/normalCube", si.faces);

        si = SMesh.LoadStructInfoOGG(SMesh.testStruct1);
        ct.meshTypes.Add("test/str1", si.mesh);
        ct.meshFaces.Add("test/str1", si.faces);
        
        ct.log.Write("Center","Finishes to load the center");

        ct.mod.OnStart();

        ct.onChunkPositionChange += () => ct.chunkLoader.Loader(ct.ppc,ct.curWorldRule.loadRadius);
        //load a time
        ct.OnChunkPositionChange();
    }
    private void Update()
    {
        var v = mouseD.ReadValue<Vector2>();             //get mouse position delta
        ct.mouseDirection = v;       //  Get mouse data
       //ct.mousePosition += v;                           //
        ct.mouseCanMove = v != Vector2.zero;

        if (ct.mouseCanMove)
        {
            ct.mousecast.Casting();
        }
    }
    private void Awake()
    {
        //ct.act = new Actions();
    }
    private void OnEnable()
    {
        //ct.act.Enable();
        ct.LockMouse();
    }
    private void OnDisable()
    {
        //ct.act.Disable();
        ct.UnlockMouse();

        Data.CreateFile(fp,ct.setting.settingPath,false);//update every disable the tetting path
        Data.WriteJson(ct.setting, ct.setting.settingPath);
        
        foreach (var si in ct.acts.Values)//disable all actions
            si.Dispose();

        ct.mod.OnFinish();

        ct.log.Write("Finish logging");
        ct.log.Stop();

    }
}
