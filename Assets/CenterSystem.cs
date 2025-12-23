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
        ct.action.AddBiding(mouseD,Action.keyTable["mouseDelta"]);

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

        ct.log.Write("Finish logging");
        ct.log.Stop();

    }
}
