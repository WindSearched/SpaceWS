using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/*
Page 和Mode 应的区别：
page 应有进入和进出的逻辑，且为全局，同时间只能进入一个page，切换时调用旧的进出和新的进入逻辑
mode 应为布尔值，且只存有一个逻辑，通过布尔判断逻辑，可为多个同时作用；可分为两个方法分别为判断（返回布尔）和执行方法（引入布尔）
	*/

public class Mode
{
	public bool currentActivation;
	public string type;
	public Condition cond;
	public ModeMeth meth;

	public Mode(string type, Condition cond, ModeMeth meth)
	{
		this.type = type;
		this.cond = cond;
		this.meth = meth;
	}

	public Mode(string type)
	{
		this.type = type;
	}

	public void Set()
	{
		meth?.Invoke(currentActivation = cond());
	}
	public void Set(bool active) => meth(currentActivation = active);
}
public delegate bool Condition();
public delegate void ModeMeth(bool  state);

public class ModeStm
{
	public Dictionary<string, Mode> modes = new();

	public void Register(string key, Mode mode)
	{
		if (modes.ContainsKey(key))
			ct.log.Write("PageStm", $"Try to register mode {key}, but this is already existed!");
		else
		{
			modes.Add(key, mode);
			ct.log.Write("PageStm", $"Register page {key}");
		}
	}

	/// <summary>
	/// active the mode with self Condition method
	/// </summary>
	/// <param name="key"></param>
	public void Active(string key) =>
		Get(key).Set();
	/// <summary>
	/// active the mode
	/// </summary>
	/// <param name="key"></param>
	/// <param name="active"></param>
	public void Active(string key, bool active) =>
		Get(key).Set(active);

	public Mode Get(string key) => modes.ContainsKey(key) ? modes[key] : null;

	/// <summary>
	/// detecte if the mode is active
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public bool IsActive(string key) => Get(key).currentActivation;
	public ModeStm()
	{
		Register("main",new (null));

		ct.command.Add("mode", (l) =>
		{
			var a1 = l.Load();
			if (l.TryLoad(out string arg))
			{
				Active(a1,bool.Parse(arg));
			}
			else
			{
				Active(a1);
			}
		});

	}
}