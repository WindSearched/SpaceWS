using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BuildPage : MonoBehaviour
{
    public Transform pageParent;

    public Transform builScrollParent;
    public SScroll buildScroll;

    public GameObject sbuttonTemplate;
    public GameObject buildStructButtonTemplate;
    public (string name, int id, GameObject structObject) buildStruct;
    public GameObject buildObject;
    public SLibrary buildObjectLibrary =>  buildObject.GetComponent<SLibrary>();

    public int wheel;

    public void FixWheel(int i)
    {
        wheel += i;
    }

    public int GetWheelIndex(int id, int max)
    {
        int a = id % max;
        return a < 0 ? max + a : a;
    }

    private void Start()
    {
        ct.buildPage = this;
        sbuttonTemplate = Resources.Load<GameObject>("ui/SButton");

        buildScroll = builScrollParent.GetComponent<SScroll>();


        ct.debugInfo.LeftAdd(() => wheel.ToString() , "wheel");

        {//create build struct button template
            var o = buildStructButtonTemplate = Instantiate(sbuttonTemplate, ct.templateParent);
            o.name = "";
            //lock to left top
            var rt = o.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new(60, 60);
            rt.anchoredPosition = Vector2.zero;//set size
            //change button

        }
        buildScroll.template = buildStructButtonTemplate;
        buildScroll.SStart();

        ct.LoadAfterInconsFinishLoading = () =>
        {
            foreach (var sp in ct.structsInfo.dict.Values
                         .SelectMany(innerDict => innerDict.Values)
                         .Select(v => v.icon))
            {
                buildScroll.Add(g =>
                {
                    g.GetComponent<Image>().sprite = sp;

                    var b = g.GetComponent<SButton>();
                    b.onButtonEnter += g =>
                    {
                        if (buildStruct.name != g.name)
                        {
                            var o = g.GetComponent<Outline>();
                            o.effectDistance = new(1, 1);
                        }
                    };
                    b.onButtonExit += g =>
                    {
                        if (buildStruct.name != g.name)
                        {
                            var o = g.GetComponent<Outline>();
                            o.effectDistance = new(0, 0);
                        }
                    };
                    b.onButtonDown += g =>
                    {
                        var o = g.GetComponent<Outline>();
                        o.effectDistance = new(3,3);

                        if (buildStruct.structObject)
                        {
                            buildStruct.structObject.GetComponent<Outline>().effectDistance = new(0,0);
                        }
                        buildStruct.structObject = g;
                        int id = buildStruct.id = int.Parse(g.name);
                        buildStruct.name = ct.structsInfo.Get(id).data.mod + "/"+ ct.structsInfo.Get(id).data.type;//save struct type
                        Debug.Log(buildStruct.name);
                    };
                    b.onButtonUp += g =>
                    {
                        var o = g.GetComponent<Outline>();
                        o.effectDistance = new(2,2);
                    };
                });
            }
        };

        ct.mousecast.InCast += g =>
        {
            if(!buildStruct.structObject) return;
            if (!g)
            {
                buildObject.SetActive(false);
            }
            else if (g.CompareTag("structFace") && ct.pages.IsPage("build"))
            {
                buildObject.SetActive(true);
                if (buildObject.name != buildStruct.name)
                {
                    var s = buildStruct.name;
                    var t = ct.structsInfo.Get(SMType.Parse(s)).template;

                    // ================================
                    // 1. MeshRenderer：只复制材质
                    // ================================
                    var srcMR = t.GetComponent<MeshRenderer>();
                    var dstMR = buildObject.GetComponent<MeshRenderer>();

                    if (srcMR != null)
                    {
                        if (dstMR == null)
                            dstMR = buildObject.AddComponent<MeshRenderer>();

                        dstMR.sharedMaterials = srcMR.sharedMaterials;

                        // 如果你要透明效果
                        if (ct.trasparentMat != null)
                        {
                            dstMR.material = ct.trasparentMat;
                            dstMR.material.color -= new Color(0, 0, 0, 0.8f);
                        }
                    }

                    // ================================
                    // 2. MeshFilter：只复制 mesh 引用
                    // ================================
                    var srcMF = t.GetComponent<MeshFilter>();
                    var dstMF = buildObject.GetComponent<MeshFilter>();

                    if (srcMF != null)
                    {
                        if (dstMF == null)
                            dstMF = buildObject.AddComponent<MeshFilter>();

                        // 关键：不要 CopyComponentTo
                        dstMF.sharedMesh = srcMF.sharedMesh;

                        // 如果你会 runtime 改 mesh（建议）
                        // dstMF.mesh = Object.Instantiate(srcMF.sharedMesh);
                        // dstMF.mesh.RecalculateBounds();
                    }

                    buildObjectLibrary.Write("type", SMType.Parse(s));
                }

                var type = g.transform.parent.GetComponent<SLibrary>().ReadValue<SMType>("type");

                var ids = ct.structsInfo.Get(buildStruct.id).connectorFaceIndexes;
                var id = ct.buildPage.GetWheelIndex(ct.buildPage.wheel, ids.Length);
                SMesh.Face.AlignFaceToFace(buildObject,ct.structsInfo.Get(buildStruct.id).faces,ids[id],g, ct.structsInfo.Get(type).faces, int.Parse(g.name));
            }
        };

    }
}
