using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static ct;

public class CenterSystem : MonoBehaviour
{
    public static string fp;
    public static InputAction move;
    public static InputAction mouseD;
    public static InputAction mouseP;

    public Material m_outline;
    public Transform templateParent;
    public RectTransform canvas;
    public DebugPage debugPage;
    private void Start()
    {
        ct.ctsym = this;

        ct.log.Write("Center","Starts to load the center");
        ct.mousecast = new();
        ct.screenSize = canvas.sizeDelta;

        move = ct.action.Add("move",InputActionType.Value);
        ct.action.AddVector2(move);
        mouseD = ct.action.Add("mouseDelta",InputActionType.Value);
        ct.action.AddBiding(mouseD,SAction.keyTable["mouseDelta"]);
        mouseP = action.Add("mouse",InputActionType.Value, SAction.keyTable["mouse"]);

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
            ct.setting = Data.FileExists(p) ? Data.ReadJson<Set>(p) : new();
        }
        //
        if(!Data.FileExists(ct.setting.exSpacePath))
            Data.DirectoryCreate(ct.setting.exSpacePath);

        ct.curWorldRule = Data.ReadJson<Rule>(ct.setting.exRulePath);

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
                ct.curWorldRule.RandFlt(cp.x - cp.y + cp.z,360, 0),
                ct.curWorldRule.RandFlt(cp.x + cp.y + cp.z,360, 0),
                ct.curWorldRule.RandFlt(cp.x - cp.y + cp.z,360, 0)
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
                bodyIndex = id,
                type = ct.structTypes[ct.curWorldRule.RandInt(cp.x - cp.y + cp.z, ct.structTypes.Count, 0)]
            });
        });
        //needed load
        m_outline.SetColor("_OutlineColour", ct.setting.outlineColor.ToColor());
        m_outline.SetFloat("_Intensity", ct.setting.outlineColorIntensity);
        m_outline.SetFloat("_OutlineWidth", ct.setting.outlineWidth);

        //default data load
        ct.defualtBody = Resources.Load("Body") as GameObject;
        ct.bodiesParent = GameObject.Find("Bodies").transform;
        ct.defaultMat = Resources.Load("DefaultMat") as Material;

        ct.debugInfo = new(ct.setting.debugLineNumber);
        debugInfo.RightAdd(() =>
        {
            var p = pp;
            return $"{p.x} {p.y} {p.z}";
        },"position");
        debugInfo.RightAdd(() =>// position of chunk where located player
        {
            var p = pcp;
            return $"{p.x} {p.y} {p.z}";
        },"chunk");
        debugInfo.RightAdd(() =>//position of mouse
        {
            var p = mousePosition;
            return $"{p.x} {p.y}";
        }, "mouse position");
        debugInfo.RightAdd(() =>//delta position of mouse
        {
            var p = mouseDirection;
            return $"{p.x} {p.y}";
        }, "mouse delta");
        debugInfo.LeftAdd(() => fps.ToString(), "fps");//fps of game

        modes.Register("alt", new("alt", () => true, active =>
        {
            if (!pages.IsPage("main")) return;
            if (active)
            {
                UnlockMouse();
                cameraCanMove = false;
            }
            else
            {
                LockMouse();
                cameraCanMove = true;
            }
        }));
        var alt = action.Add("alt", InputActionType.Button, SAction.keyTable["alt"]);
        alt.performed += _ => modes.Active("alt", true);
        alt.canceled += _ => modes.Active("alt", false);
        var lm = ct.leftMouse_act = ct.action.Add("leftMouse", InputActionType.Button, SAction.keyTable["leftMouse"]);
        lm.performed += _ =>
        {

            if (modes.IsActive("alt"))
            {
                var c = mouseCasted;
                if (c)
                {
                    var id = c.transform.parent.parent.gameObject.name;
                    Debug.Log(id);
                    var type = bodies.datas[int.Parse(id)].structs[0].type;
                    SMesh.Face.CreateFace(ct.structFaces[type]);
                    c.SetActive(false);
                }
            }
        };

        ct.log.Write("Center","Finishes to load the center");

        ct.mod.OnStart();

        if (ct.curWorldRule.chunkload)
        {
            ct.onChunkPositionChange += () => ct.chunkLoader.Loader(ct.pcp,ct.curWorldRule.loadRadius);
        }
        //load a time
        ct.OnChunkPositionChange();
    }
    private void Update()
    {
        var v = mouseD.ReadValue<Vector2>();             //get mouse position delta
        ct.mouseDirection = v;       //  Get mouse data
       //ct.mousePosition += v;                           //
        ct.mouseCanMove = v != Vector2.zero;

        var p = mouseP.ReadValue<Vector2>();
        ct.mousePosition = p;

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
        
        foreach (var si in ct.acts.Values)//disable all actions
            si.Dispose();

        ct.mod.OnFinish();

        ct.log.Write("Finish logging");
        ct.log.Stop();

    }

    private void OnApplicationQuit()
    {
        ct.UnlockMouse();

        if (ct.setting == null)
        {
            Debug.LogWarning("CenterSystem: setting == null");
            return;
        }

        Data.CreateFile(fp,ct.setting.settingPath,false);//update every disable the setting path
        Data.WriteJson(ct.setting, ct.setting.settingPath);
        ct.curWorldRule.SetJson(ct.setting.exRulePath);
    }

    private void OnRectTransformDimensionsChange()
    {
        ct.screenSize = canvas.sizeDelta;
    }
}