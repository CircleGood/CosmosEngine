﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using CosmosEngine;

/// <summary>
/// CosmosEngine标准Object,，带有自动Debug~
/// </summary>
public class CObject : IDisposable
{
    public CObject()
    {
        this.StartWatch();
    }

    public void Dispose()
    {
        this.StopWatch();
    }
}
/// <summary>
/// 手动打开或关闭，用于任何object
/// </summary>
public static class CObjectDebuggerExtensions
{
    public static void StartWatch(this object obj)
    {
        CObjectDebugger.StartWatch(obj);
    }

    public static void StopWatch(this object obj)
    {
        CObjectDebugger.StopWatch(obj);
    }
}

/// <summary>
/// 对C#非MonoBehaviour对象以GameObject形式表现，方便调试
/// </summary>
public class CObjectDebugger : MonoBehaviour
{
    public static Dictionary<object, CObjectDebugger> Cache = new Dictionary<object, CObjectDebugger>();
    public static IEnumerator GlobalDebugCoroutine;  // 不用Update，用这个~

    public const string ContainerName = "CObjectDebugger";
    public object WatchObject;
    public List<string> DebugStrs = new List<string>();
    private GameObject _cacheGameObject;

    public static void StopWatch(object obj)
    {
        CAsync.AddMainThreadCall(() =>
        {

            try
            {
                if (!CDebug.IsEditor)
                    return;

                CObjectDebugger debuger;
                if (CObjectDebugger.Cache.TryGetValue(obj, out debuger))
                {
                    GameObject.Destroy(debuger.gameObject);
                }
            }
            catch (Exception e)
            {
                CDebug.LogError(e.Message);
            }
 
        });
    }

    public static void StartWatch(object obj)
    {
        CAsync.AddMainThreadCall(() =>
        {
            try
            {
                if (!CDebug.IsEditor)
                    return;

                var newDebugger = new GameObject(string.Format("{0}-{1}", obj.ToString(), obj.GetType())).AddComponent<CObjectDebugger>();
                newDebugger.WatchObject = obj;

                CDebuggerObjectTool.SetParent(ContainerName, obj.GetType().Name, newDebugger.gameObject);

                Cache[obj] = newDebugger;
            }
            catch (Exception e)
            {
                CDebug.LogError(e.Message);
            }

        });
    }

    void Awake()
    {
        if (!CDebug.IsEditor)
        {
            CDebug.LogError("Error Open CObjectDebugger on not Unity Editor");
            return;
        }
        _cacheGameObject = gameObject;
        if (GlobalDebugCoroutine == null)
        {
            GlobalDebugCoroutine = CoGlobalDebugCoroutine();
            CCosmosEngine.EngineInstance.StartCoroutine(GlobalDebugCoroutine);
        }
    }

    /// <summary>
    /// 主要为了清理和改名
    /// </summary>
    /// <returns></returns>
    static IEnumerator CoGlobalDebugCoroutine()
    {
        while (true)
        {
            if (Cache.Count <= 0)
            {
                yield return null;
                continue;
            }
            var copyCache = new Dictionary<object, CObjectDebugger>(Cache);
            foreach (var kv in copyCache)
            {
                var debugger = kv.Value;
                if (debugger.WatchObject == null)
                {
                    GameObject.Destroy(debugger._cacheGameObject);
                }
                else
                {
                    if (debugger._cacheGameObject.name != debugger.WatchObject.ToString())
                    {
                        debugger._cacheGameObject.name = debugger.WatchObject.ToString();
                    }
                }
                yield return null;
            }
        }

    }
}
