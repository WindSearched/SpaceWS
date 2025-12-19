using UnityEngine;

public class CenterSystem : MonoBehaviour
{
    public static string fp;
    private void Start()
    {
        ct.log.Write("Center","Starts to load the center");
        
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

                var p = ct.act.main.move.ReadValue<Vector2>();// player position
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
        var v = ct.act.main.mouse.ReadValue<Vector2>(); //
        ct.mouseDirection = v - ct.mousePosition;       //  Get mouse data
        ct.mousePosition = v;                           //  
        ct.mouseCanMove = ct.mouseDirection != Vector2.zero;

    }
    private void Awake()
    {
        ct.act = new Actions();
    }
    private void OnEnable()
    {
        ct.act.Enable();
    }
    private void OnDisable()
    {
        ct.act.Disable();
        Data.CreateFile(fp,ct.setting.settingPath,false);//update every disable the tetting path
        Data.WriteJson(ct.setting, ct.setting.settingPath);
        
        foreach (var si in ct.acts.Values)//disable all actions
            si.Dispose();

        ct.log.Write("Finish logging");
        ct.log.Stop();
    }
}
