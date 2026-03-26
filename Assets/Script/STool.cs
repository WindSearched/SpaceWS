using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MemoryPack;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// This class store some messy(not include in SMath and SMesh) tool method
/// </summary>
public static class STool
{
    /// <summary>
    /// 复制源组件到目标物体，如果目标已有同类型组件则覆盖属性
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="source">源组件</param>
    /// <param name="destination">目标物体</param>
    /// <param name="instantiateReferences">是否实例化引用类型字段（Sprite/Material 等）</param>
    /// <returns>目标物体上的组件</returns>
    public static T CopyComponentTo<T>(T source, GameObject destination, bool instantiateReferences = false) where T : Component
    {
        if (typeof(T) == typeof(Transform) || source == null || destination == null)
            return null;

        // 获取目标已有组件
        T targetComp = destination.GetComponent<T>();
        if (targetComp == null)
        {
            // 没有则添加
            targetComp = destination.AddComponent<T>();
        }

        System.Type type = typeof(T);

        // ----------------- 复制字段 -----------------
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            object value = field.GetValue(source);
            if (instantiateReferences && value is Object unityObj)
            {
                // 对 UnityEngine.Object 类型做实例化
                value = Object.Instantiate(unityObj);
            }
            field.SetValue(targetComp, value);
        }

        // ----------------- 复制属性 -----------------
        PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            if (!prop.CanWrite) continue;   // 只写可写属性
            if (prop.GetIndexParameters().Length > 0) continue; // 跳过索引器
            try
            {
                object value = prop.GetValue(source, null);
                if (instantiateReferences && value is Object unityObj)
                {
                    value = Object.Instantiate(unityObj);
                }
                prop.SetValue(targetComp, value, null);
            }
            catch
            {
                // 某些只读属性或内部属性会抛异常，忽略
            }
        }

        return targetComp;
    }

    public static class TypeConverterUtil
    {
        public static object ConvertFromString(Type type, string value)
        {
            if (type == typeof(string))
                return value;

            if (string.IsNullOrEmpty(value))
                return null;

            // 基础类型
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(float)) return float.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(bool)) return bool.Parse(value);
            if (type == typeof(long)) return long.Parse(value);

            // 枚举
            if (type.IsEnum)
                return Enum.Parse(type, value);

            // List<T>（用逗号分隔）
            if (typeof(IList<>).IsAssignableFrom(type))
            {
                var list = (IList)Activator.CreateInstance(type);
                Type elementType = type.GetGenericArguments()[0];

                var parts = value.Split(',');

                foreach (var p in parts)
                {
                    list.Add(ConvertFromString(elementType, p.Trim()));
                }

                return list;
            }

            // struct / class（简单支持：用 "x,y,z" 形式）
            if (type.IsValueType || type.IsClass)
            {
                var obj = Activator.CreateInstance(type);
                var fields = type.GetFields();

                var parts = value.Split(',');

                for (int i = 0; i < fields.Length && i < parts.Length; i++)
                {
                    var field = fields[i];
                    object fieldValue = ConvertFromString(field.FieldType, parts[i].Trim());
                    field.SetValue(obj, fieldValue);
                }

                return obj;
            }

            // fallback
            return Convert.ChangeType(value, type);
        }
    }
}

[MemoryPackable][Serializable]
public partial struct SMType
{
    public string type;
    public string mod;

    public SMType(string type, string mod)
    {
        this.type = type;
        this.mod = mod;
    }
    public override string ToString() => mod + "/" + type;
    public static bool TryParse(string s, out  SMType smt)
    {
        smt = default;
        if (!s.Contains("/")) return false;
        var ss = s.Split('/');
        if (ss.Length > 2) return false;
        smt = new SMType(ss[1], ss[0]);
        return true;
    }

    public static SMType Parse(string s)
    {
        var ss = s.Split('/');
        return new SMType(ss[1], ss[0]);
    }


    public bool IsNull() => type == null && mod == null;
}