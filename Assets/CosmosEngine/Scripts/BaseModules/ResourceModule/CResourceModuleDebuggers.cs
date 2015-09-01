﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 专门用于资源Debugger用到的父对象自动生成
/// 
/// DebuggerObject - 用于管理虚拟对象（只用于显示调试信息的对象）
/// </summary>
public class CDebuggerObjectTool
{
    private static readonly Dictionary<string, Transform> Parents = new Dictionary<string, Transform>();
    private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>(); // 数量统计...

    static string GetUri(string bigType, string smallType)
    {
        var uri = string.Format("{0}/{1}", bigType, smallType);
        return uri;
    }

    public static void SetParent(string bigType, string smallType, GameObject obj)
    {
        var uri = GetUri(bigType, smallType);
        Transform theParent = GetParent(bigType, smallType);

        int typeCount;
        if (!Counts.TryGetValue(uri, out typeCount))
        {
            Counts[uri] = 0;
        }
        typeCount = ++Counts[uri];

        CTool.SetChild(obj, theParent.gameObject);

        theParent.gameObject.name = GetNameWithCount(smallType, typeCount);

    }

    public static void RemoveFromParent(string bigType, string smallType, GameObject obj)
    {
        if (!CBehaviour.IsApplicationQuited)
        {
            if (obj != null)
                GameObject.Destroy(obj);

        }

        var newCount = --Counts[GetUri(bigType, smallType)];
        if (!CBehaviour.IsApplicationQuited)
        {
            GetParent(bigType, smallType).gameObject.name = GetNameWithCount(smallType, newCount);
        }
    }

    /// <summary>
    /// 设置Parent名字,带有数量
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="smallType"></param>
    /// <param name="count"></param>
    protected static string GetNameWithCount(string smallType, int count)
    {
        return string.Format("{0}({1})", smallType, count);
    }

    protected static Transform GetParent(string bigType, string smallType)
    {
        var uri = GetUri(bigType, smallType);
        Transform theParent;

        if (!Parents.TryGetValue(uri, out theParent))
        {
            var bigTypeObjName = string.Format("__{0}__", bigType);
            var bigTypeObj = GameObject.Find(bigTypeObjName) ?? new GameObject(bigTypeObjName);

            theParent = new GameObject(smallType).transform;
            CTool.SetChild(theParent, bigTypeObj.transform);
            Parents[uri] = theParent;
        }
        return theParent;
    }
}

// 只在编辑器下出现，分别对应一个Loader~生成一个对象，为了方便调试！
public class CResourceLoaderDebugger : MonoBehaviour
{
    public CBaseResourceLoader TheLoader;
    public int RefCount;
    public float FinishUsedTime; // 参考，完成所需时间

    public static CResourceLoaderDebugger Create(string type, string url, CBaseResourceLoader loader)
    {
        const string bigType = "ResourceLoaders";

        Func<string> getName = () => string.Format("{0}-{1}-{2}", type, loader.Desc, url);

        var newHelpGameObject = new GameObject(getName());
        CDebuggerObjectTool.SetParent(bigType, type, newHelpGameObject);
        var newHelp = newHelpGameObject.AddComponent<CResourceLoaderDebugger>();
        newHelp.TheLoader = loader;

        loader.SetDescEvent += (newDesc) =>
        {
            if (loader.RefCount > 0)
                newHelpGameObject.name = getName();
        };

        loader.DisposeEvent += () =>
        {
            CDebuggerObjectTool.RemoveFromParent(bigType, type, newHelpGameObject);
        };
        

        return newHelp;
    }

    void Update()
    {
        RefCount = TheLoader.RefCount;
        FinishUsedTime = TheLoader.FinishUsedTime;
    }
}

public class CResourceLoadObjectDebugger : MonoBehaviour
{
    public UnityEngine.Object TheObject;
    const string bigType = "LoadedObjects";
    public string Type;
    private bool IsRemoveFromParent = false;
    public static CResourceLoadObjectDebugger Create(string type, string url, UnityEngine.Object theObject)
    {
        var newHelpGameObject = new GameObject(string.Format("LoadedObject-{0}-{1}", type, url));
        CDebuggerObjectTool.SetParent(bigType, type, newHelpGameObject);

        var newHelp = newHelpGameObject.AddComponent<CResourceLoadObjectDebugger>();
        newHelp.Type = type;
        newHelp.TheObject = theObject;
        return newHelp;
    }

    void Update()
    {
        if (TheObject == null && !IsRemoveFromParent)
        {
            CDebuggerObjectTool.RemoveFromParent(bigType, Type, gameObject);
            IsRemoveFromParent = true;
        }
    }
    // 可供调试删资源
    void OnDestroy()
    {
        if (!IsRemoveFromParent)
        {
            CDebuggerObjectTool.RemoveFromParent(bigType, Type, gameObject);
            IsRemoveFromParent = true;
        }

    }
}