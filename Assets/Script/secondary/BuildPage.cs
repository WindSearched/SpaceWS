using System;
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
    public (string name, GameObject structObject) buildStruct;
    public GameObject buildObject;
    public SLibrary buildObjectLibrary =>  buildObject.GetComponent<SLibrary>();
    private void Start()
    {
        ct.buildPage = this;
        sbuttonTemplate = Resources.Load<GameObject>("ui/SButton");

        buildScroll = builScrollParent.GetComponent<SScroll>();

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
            foreach (var sp in ct.structIcons.Values)
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
                        buildStruct.name = g.name;//save struct type
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
                    var t = ct.structsInfo.Get(s).template;
                    var m = STool.CopyComponentTo(t.GetComponent<MeshRenderer>(), buildObject);
                    m.material = ct.trasparentMat;
                    m.material.color -= new Color(0, 0, 0, 0.8f);
                    STool.CopyComponentTo(t.GetComponent<MeshFilter>(), buildObject);

                    buildObjectLibrary.Write("type", s);
                }

                var type = g.transform.parent.name.Split(' ')[1];
                SMesh.Face.AlignFaceToFace(buildObject,ct.structFaces[buildObjectLibrary.Read("type") as string],0,g, ct.structFaces[type], int.Parse(g.name));
            }
        };

    }
}
