using UnityEngine;

public class CenterSystem : MonoBehaviour
{
    private void Start()
    {
        string fp = Application.persistentDataPath + "/setpath";
        if (!Data.FileExists(fp))
        {
            ct.setting = new();
            Data.CreateFile(fp,ct.setting.settingPath,false);
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
        ct.meshTypes.Add("test/normalCube", SMesh.LoadMeshFromTextOBJ(SMesh.cubeOBJ));
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
    }

}
