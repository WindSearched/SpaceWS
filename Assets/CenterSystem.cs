using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static ct;

public class CenterSystem : MonoBehaviour
{
	public static string fp;
	public static InputAction move;
	public static InputAction mouseD;
	public static InputAction mouseP;
	public static InputAction mouseWheel;
	public int wheelV;
	private bool wheelTiking;
	/// <summary>
	/// can invoke every 5 tick
	/// </summary>
	public event Action<int> WhenWhellPerTicks;

	public Material m_outline;
	public Material trasparentMat;
	public Transform templateParent;
	public Transform structFacesTemplateParent;
	public RectTransform canvas;
	public DebugPage debugPage;
	public GameObject escPage;
	public GameObject buildPage;
	public Camera projectCamera;
	public Transform projectedParent;
	public RenderTexture projectTexture;
	public GameObject player;
	public InfoViewer infoViewer;

	public event Meth WhenIconsFinisheLoading;
	public void IWhenIconsFinisheLoading() => WhenIconsFinisheLoading?.Invoke();


	public bool finishLoadIcon = false;
	private void Start()
	{
		ct.ctsym = this;
		ct.camera = Camera.main;

		ct.log.Write("Center","Starts to load the center");
		ct.mousecast = new();
		ct.screenSize = canvas.sizeDelta;

		Projector.previewCamera = projectCamera;
		Projector.parent = projectedParent;
		Projector.renderTexture = projectTexture;

		move = ct.action.Add("move",InputActionType.Value);
		ct.action.AddVector2(move);
		mouseD = ct.action.Add("mouseDelta",InputActionType.Value);
		ct.action.AddBiding(mouseD,SAction.keyTable["mouseDelta"]);
		mouseP = action.Add("mouse",InputActionType.Value, SAction.keyTable["mouse"]);

		mouseWheel = action.Add("wheel",InputActionType.Value, SAction.keyTable["mScroll"]);

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

		Tick.Reg( _ =>
			{
				ct.UpdatePerTick();

				var p = move.ReadValue<Vector2>();// player move diretion
				ct.plyComp.moving = p != Vector2.zero;
				ct.wasdDirection = p;

				ct.fps = 1f / Time.unscaledDeltaTime;
			}, 1, -1, 1
			);

		ct.chunkLoader = new(ct.bodies);
		//needed load
		m_outline.SetColor("_OutlineColour", ct.setting.outlineColor.ToColor());
		m_outline.SetFloat("_Intensity", ct.setting.outlineColorIntensity);
		m_outline.SetFloat("_OutlineWidth", ct.setting.outlineWidth);

		//default data load
		ct.defualtBody = Resources.Load("Body") as GameObject;
		ct.bodiesParent = GameObject.Find("Bodies").transform;
		ct.defaultMat = Resources.Load("DefaultMat") as Material;

		debugInfo:
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
		debugInfo.LeftAdd(() => Tick.tickCount.ToString(), "tick");
		debugInfo.LeftAdd(() => wheelV.ToString() , "mouseWheel");
		debugInfo.LeftAdd(() => Tick.tickS.tickEventCount.ToString() , "tickEventCount");

		inputRegistering:
		modes.Register("esc", new Mode("esc",() => false, active =>
		{
			cameraMove.Pin("esc", active);
			MouseLocking(!active);
		}));
		pages.Register("esc", new(_ =>
		{//on open
			escPage.SetActive(true);
		}, _ =>
		{// on close
			escPage.SetActive(false);
		}));
		var esc = action.Add("esc",  InputActionType.Button, SAction.keyTable["esc"]);
		esc.performed += _ =>
		{
			if (pages.IsPage("main"))
			{
				modes.Active("esc", true);
				pages.Swap("esc");
			}
			else
			{
				pages.Swap("main");
				modes.Active("esc", false);
			}
		};
		modes.Register("alt", new("alt", () => true, active =>//regist a mode named alt
		{
			cameraMove.Pin("alt", active);
			MouseLocking(!active);
		}));
		var alt = action.Add("alt", InputActionType.Button, SAction.keyTable["alt"]);
		alt.performed += _ =>
		{
			modes.Active("alt", true);
		};
		alt.canceled += _ =>
		{
			modes.Active("alt", false);
		};
		pages.Register("build", new(s =>
		{
			buildPage.SetActive(true);

			var c = mouseCasted;
			if (c)
			{
				//load objects
				var id = c.transform.parent.parent.gameObject.name;//index of struct's body
				int strid = int.Parse(c.name);//index of struct in whose body
				var type = bodies.datas[int.Parse(id)].structs[strid].type;//get object type
				var g = Instantiate(structsInfo.Get(type).connectorTamplate);
				var lib = g.AddComponent<SLibrary>();
				lib.Write("bodyIndex", int.Parse(id));
				lib.Write("type", type);
				lib.Write("structIndex", strid);
				g.name = type.ToString();
				g.SetActive(true);
				g.transform.SetPositionAndRotation(c.transform.position, c.transform.rotation);
				c.GetComponent<MeshCollider>().enabled = false;
				ct.sCamera.ChangeTarget(g);

				ct.buildPage.wheel = 0;
				WhenWhellPerTicks += ct.buildPage.FixWheel;
				//save
				s.storer.Add("str", c);//save true struct
				s.storer.Add("faced", g);// the fake(faced) struct

				playerMove.AddPin("build");
			}
		}, s =>
		{
			buildPage.SetActive(false);
			//modes.Active("esc", false);

			var str = s.storer.Get("str") as GameObject;
			var faced = s.storer.Get("faced") as GameObject;

			str.GetComponent<MeshCollider>().enabled = true;
			faced.SetActive(false);
			Destroy(faced);

			WhenWhellPerTicks -= ct.buildPage.FixWheel;

			sCamera.ChangeTarget(ct.player);

			s.storer.Clear();

			playerMove.RemovePin("build");
		}));
		var lm = ct.leftMouse_act = ct.action.Add("leftMouse", InputActionType.Button, SAction.keyTable["leftMouse"]);
		lm.performed += _ =>
		{
			if(!mouseCasted || !mouseCasted.CompareTag("struct"))
				return;
			if (modes.IsActive("alt") && !pages.IsPage("build"))
			{
				pages.Swap("build");
			}
		};
		var rm = rightMouse_act = action.Add("rightMouse", InputActionType.Button, SAction.keyTable["rightMouse"]);
		rm.performed += _ =>
		{
			var c = mouseCasted;
			if (pages.IsPage("build"))
			{
				if (!c || !c.CompareTag("structFace")) return;
				int fid = int.Parse(c.name); //face index
				SLibrary lib = c.transform.parent.GetComponent<SLibrary>();
				int bid = lib.ReadValue<int>("bodyIndex");
				int sid = lib.ReadValue<int>("structIndex");
				var based = ct.GetState(new(bid, sid));
				var state = new StructState(ct.buildPage.buildObjectLibrary.ReadValue<SMType>("type"))
				{
					bodyIndex = bid,
					isStruct = true
				};

				var ids = ct.structsInfo.Get(state.type).connectorFaceIndexes;
				var id = ct.buildPage.GetWheelIndex(ct.buildPage.wheel, ids.Length);

				bodies.LoadStructOn(state,fid,based, ids[id]);
			}
			else if (pages.IsPage("main"))
			{
				if (c && c.CompareTag("struct"))
				{
					dLog("Enter in meth");
					int sid = int.Parse(c.name);
					int bid = int.Parse(c.transform.parent.parent.name);
					StructIdPath iph = new(bid, sid);

					var state = ct.GetState(iph);
					if (!ct.GetData(state).isContainer_) return;
					var elements = ct.viewPage.modifiers.Get("container", "main").Invoke(state.container);
					ct.viewPage.Add(elements);
					ct.viewPage.targetPath = iph;
					viewPage.lib.Write("idpath", iph);
					pages.Swap("view");
				}
			}
		};

		materials.Set("hoonk", "main", new MaterialData() // 焢
		{
			density = 1145,
			fusionPoint = 9999,
			mod = "main",
			specificHeat = 7900,
			type = "hoonk"
		});

		ct.debugInfo.LeftAdd(() => ct.playerMove.loosing.ToString(), "player can move" );

		ct.log.Write("Center","Finishes to load the center");

		ct.mod.OnStart();

		if (ct.curWorldRule.chunkload)
		{
			ct.onChunkPositionChange += () => ct.chunkLoader.Loader(ct.pcp,ct.curWorldRule.loadRadius);
		}

		var info = ct.action.Add("Info", InputActionType.Button, SAction.keyTable["i"]);
		info.performed += _ =>
		{
			infoViewer.gameObject.SetActive(!infoViewer.gameObject.activeSelf);
		};
		mousecast.InCast += o =>
		{
			List<string> list = null;
			if (!o) return;

			if (pages.IsPage("main"))
			{
				if (o.CompareTag("struct"))
				{
					list = new List<string>
					{
						"type", "bodyIndex"
					};
					int bid = int.Parse(o.transform.parent.parent.name);
					int sid = int.Parse(o.name);
					var s = bodies.datas[bid].structs[sid];
					var d = ct.structsInfo.Get(s.type).data;

					if(d.isFactory_)
						list.Add("stuffList");

					infoViewer.AddViews(s, list);
				}
			}
			else if (pages.IsPage("build"))
			{
				if (o.CompareTag("structFace"))
				{
					infoViewer.AddView("faceIndex",o.name);
				}
			}

			infoViewer.Updating();
		};
		mousecast.OutCast += o =>
		{
			if (!o) return;
			void clear() => infoViewer.Clear(true);

			if (pages.IsPage("main"))
			{
				if (o.CompareTag("struct"))
				{
					clear();
				}
			}
			else if (pages.IsPage("build"))
			{
				if (o.CompareTag("structFace"))
				{
					clear();
				}
			}
		};

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

		wheelV = (int)mouseWheel.ReadValue<Vector2>().y;

		if (!wheelTiking && wheelV != 0)
		{
			wheelTiking = true;
			WhenWhellPerTicks?.Invoke(wheelV);

			Tick.Reg(t =>
			{
				if (wheelV == 0)
				{
					wheelTiking = false;
					t.Stop();

				}
				else
					WhenWhellPerTicks?.Invoke(wheelV);
			}, 1, -1, 1);
		}
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