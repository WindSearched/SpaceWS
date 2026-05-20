using System;
using System.Collections.Generic;
using UnityEngine;

public class ViewPage : MonoBehaviour
{
	public SMTDict<ViewElementBase> elementbases = new();
	public SMTDict<Func<object,List<ViewElementBase>>> modifiers = new ();
	public List<ViewElement> elements = new();
	/// <summary>
	/// index of obj that be removed in next refresh
	/// </summary>
	public List<ViewElement> removed;
	public int loadCount;

	private GameObject elTemp;
	public SLib lib = new();

	public StructIdPath targetPath;

	private void Start()
	{
		ct.viewPage = this;
		elTemp = Resources.Load<GameObject>("ui/ViewElement");

		modifiers.Set(new("container", "main"), (o) =>
		{
			var con = o as StructState.Contain;
			List<ViewElementBase> lb = new();

			ViewElementBase base_ = new();
			var l = con.container.GetList();
			Vector2 init = new(-200, 200);
			int size = 20;
			int linecount = 10;

			for (int i = 0; i < l.Count; i++)
			{
				var am = l[i];
				var iy = i / linecount;
				var ix = i % linecount;

				ViewElementBase b = new();
				b.position = init;
				b.textColor = Color.black;
				b.backColor =  Color.white;
				b.size = new Vector2(size, size);
				b.offset = new Vector2(ix, -iy) * size;
				b.pivot = new(-1, 1);
				b.text = am.ToString();

				lb.Add(b);
			}
			ViewElementBase but = new();
			but.textColor = Color.black;
			but.backColor =  Color.white;
			but.position = new(0, 210);
			but.size = new(100, 10);
			but.pivot = new(0.5f, 0.5f);
			but.text = "add a test item";
			but.downInput = (e, g) =>
			{
				var iph = lib.ReadValue<StructIdPath>("idpath");
				var state = ct.GetState(iph);
				var data = ct.GetData(state);

				if (data.isContainer_)
				{
					SMType t = new("testitem", "main");
					state.container.container.SetUnlock(t, true);
					state.container.AddItem(t, 1, out _);
					ct.bodies.Update(state._idPath);
				}
				ct.viewPage.Clear(false);
				ct.viewPage.Add(new("container", "main"), o,false);
				ct.viewPage.Refresh();
			};
			lb.Add(but);

			return lb;
		});

		ct.pages.Register("view", new(
			g =>
			{
				ct.playerMove.AddPin("viewpage");
				ct.cameraMove.AddPin("viewpage");
				ct.MouseLocking(false);

				gameObject.SetActive(true);
				Refresh();
				ct.viewPage.gameObject.SetActive(true);
			},
			g =>
			{
				ct.playerMove.RemovePin("viewpage");
				ct.cameraMove.RemovePin("viewpage");
				ct.MouseLocking(false);

				gameObject.SetActive(true);
				ct.viewPage.Clear(false);
				ct.viewPage.gameObject.SetActive(false);
			}
			));
	}

	public GameObject FindVoid()
	{
		int c = transform.childCount -1;
		if (c < 0)
			return null;
		var last = transform.GetChild(c);
		return !last.gameObject.activeSelf ? last.gameObject : null;
	}

	public void Add(ViewElementBase b, bool singleupdate = true)
	{
		var g = FindVoid();
		if (!g)// is null
			g = Instantiate(elTemp, transform);
		else
		{
			g.SetActive(true);
		}
		g.transform.SetAsFirstSibling();
		g.name = loadCount++.ToString();
		var e = g.GetComponent<ViewElement>();
		e.SetBase(b, singleupdate);
		elements.Add(e);
	}

	public void Add(List<ViewElementBase> list, bool singleupdate = true)
	{
		foreach (var e in list)
			Add(e, singleupdate);
	}

	public void Add(SMType type, object obj, bool singleupdate = true)
	{
		Add(modifiers.Get(type)?.Invoke(obj), singleupdate);
	}

	public void Refresh()
	{
		foreach (var el in removed)
		{
			Remove(el);
		}
		removed.Clear();
		foreach (var e in elements)
		{
			e.Refresh();
		}
	}

	public void Remove(int index, bool singlerefresh = true)
	{
		var e = elements[index];
		if (singlerefresh)
		{
			Remove(e);
		}
		else
		{
			removed.Add(e);
		}
	}

	public void Remove(ViewElement e, bool removenow = true)
	{
		if (removenow)
		{
			e.gameObject.SetActive(false);
			e.transform.SetAsLastSibling();
			e.updatestop = true;
			elements.Remove(e);
		}
		else
		{
			removed.Add(e);
		}
	}

	public void Clear(bool refresh = true)
	{
		Action<int> act = refresh ?
			i => Remove(i, elements[i]) :
			i => removed.Add(elements[i]);

		for (int i = 0; i < elements.Count; i++)
		{
			act.Invoke(i);
		}
	}
}

public struct ViewElementBase
{
	public Vector2 size;
	public Vector2 position;
	public Vector2 offset;
	public Vector2 pivot;

	public string text;
	public Color textColor;
	public Color backColor;
	public Sprite sprite;

	public Action<ViewElement, GameObject> enterInput;
	public Action<ViewElement, GameObject> exitInput;
	public Action<ViewElement, GameObject> downInput;
	public Action<ViewElement, GameObject> upInput;

	public Action<ViewElement> update;
}