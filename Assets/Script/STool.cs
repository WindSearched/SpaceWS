using System.Reflection;
using UnityEngine;

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
}