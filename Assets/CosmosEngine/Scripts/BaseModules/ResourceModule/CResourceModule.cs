﻿//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------

using System;
using CosmosEngine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Object = UnityEngine.Object;

public enum CResourceManagerPathType
{
    StreamingAssetsPathPriority, // 忽略PersitentDataPath
    PersistentDataPathPriority,  // 尝试在Persistent目錄尋找，找不到再去StreamingAssets
}
public class CResourceModule : MonoBehaviour, ICModule
{
    public delegate void ASyncLoadABAssetDelegate(Object asset, object[] args);
    public enum LoadingLogLevel
    {
        None,
        ShowTime,
        ShowDetail,
    }

    private static CResourceModule _Instance;
    public static CResourceModule Instance
    {
        get
        {
            if (_Instance == null)
            {
                GameObject resMgr = GameObject.Find("_ResourceModule_");
                if (resMgr == null)
                    resMgr = new GameObject("_ResourceModule_");

                _Instance = resMgr.AddComponent<CResourceModule>();
            }
            return _Instance;
        }
    }
    public static bool LoadByQueue = false;
    public static int LogLevel = (int)LoadingLogLevel.None;
    public static string BuildPlatformName { get { return GetBuildPlatformName(); } }
    public static string FileProtocol { get { return GetFileProtocol(); } }  // for WWW...with file:///xxx

    /// <summary>
    /// Product Folder's Relative Path   -  Default: ../Product,   which means Assets/../Product
    /// </summary>
    public static string ProductRelPath { get { return CCosmosEngine.GetConfig("ProductRelPath"); } }

    /// <summary>
    /// Product Folder Full Path , Default: C:\xxxxx\xxxx\../Product
    /// </summary>
    public static string ProductFullPath { get { return Path.Combine(Application.dataPath, ProductRelPath); } }

    public static string ResourcesPath;
    public static string ResourcesPathWithoutFileProtocol;
    public static string ApplicationPath;
    public static string DocumentResourcesPathWithoutFileProtocol;
    public static string DocumentResourcesPath;

    public static CResourceManagerPathType ResourcePathType = CResourceManagerPathType.PersistentDataPathPriority;  // 是否優先找下載的資源?還是app本身資源優先

    public static System.Func<string, string> CustomGetResourcesPath; // 自定义资源路径。。。

    /// <summary>
    /// 统一在字符串后加上.box, 取决于配置的AssetBundle后缀
    /// </summary>
    /// <param name="path"></param>
    /// <param name="formats"></param>
    /// <returns></returns>
    public static string GetAssetBundlePath(string path, params object[] formats)
    {
        return string.Format(path + CCosmosEngine.GetConfig("AssetBundleExt"), formats);
    }

    // 检查资源是否存在
    public static bool ContainsResourceUrl(string resourceUrl)
    {
        string fullPath;
        return GetResourceFullPath(resourceUrl, out fullPath, false);
    }

    /// <summary>
    /// 完整路径，www加载
    /// </summary>
    /// <param name="url"></param>
    /// <param name="isLog"></param>
    /// <returns></returns>
    public static string GetResourceFullPath(string url, bool isLog = true)
    {
        string fullPath;
        if (GetResourceFullPath(url, out fullPath, isLog))
            return fullPath;

        return null;
    }

    public static bool GetResourceFullPath(string url, out string fullPath, bool isLog = true)
    {
        if (string.IsNullOrEmpty(url))
            CDebug.LogError("尝试获取一个空的资源路径！");

        string docUrl;
        bool hasDocUrl = TryGetDocumentResourceUrl(url, out docUrl);

        string inAppUrl;
        bool hasInAppUrl = TryGetInAppResourceUrl(url, out inAppUrl);

        if (ResourcePathType == CResourceManagerPathType.PersistentDataPathPriority)  // 優先下載資源模式
        {
            if (hasDocUrl)
            {
                if (Application.isEditor)
                    CDebug.LogWarning("[Use PersistentDataPath] {0}", docUrl);
                fullPath = docUrl;
                return true;
            }
            // 優先下載資源，但又沒有下載資源文件！使用本地資源目錄 
        }

        if (!hasInAppUrl) // 连本地资源都没有，直接失败吧 ？？ 沒有本地資源但又遠程資源？竟然！!?
        {
            if (isLog)
                CDebug.LogError("[Not Found] InApp Url Resource: {0}", url);
            fullPath = null;
            return false;
        }

        fullPath = inAppUrl;  // 直接使用本地資源！

        return true;
    }

    /// <summary>
    /// 獲取app數據目錄，可寫，同Application.PersitentDataPath，但在windows平台時為了避免www類中文目錄無法讀取問題，單獨實現
    /// </summary>
    /// <returns></returns>
    public static string GetAppDataPath()
    {
        // Windows 时使用特定的目录，避免中文User的存在
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsWebPlayer)
        {
            string dataPath = Application.dataPath + "/../Temp/UnityWinPersistentDataPath";
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
            return dataPath;
        }

        return Application.persistentDataPath;
    }

    // (not android ) only! Android资源不在目录！
    public static bool TryGetInAppResourceUrl(string url, out string newUrl)
    {
        newUrl = ResourcesPath + url;

        // 注意，StreamingAssetsPath在Android平台時，壓縮在apk里面，不要做文件檢查了
        if (Application.platform != RuntimePlatform.Android && !File.Exists(ResourcesPathWithoutFileProtocol + url))
        {
            return false;
        }

        return true;
    }

    public static bool TryGetDocumentResourceUrl(string url, out string newUrl)
    {
        newUrl = DocumentResourcesPath + url;
        if (File.Exists(DocumentResourcesPathWithoutFileProtocol + url))
        {
            return true;
        }

        return false;
    }

    void Awake()
    {
        if (_Instance != null)
            CDebug.Assert(_Instance == this);

        //InvokeRepeating("CheckGcCollect", 0f, 3f);
    }

    void Update()
    {
        CBaseResourceLoader.CheckGcCollect();
    }
    public IEnumerator Init()
    {
        InitResourcePath();

        yield break;
    }

    public IEnumerator UnInit()
    {
        yield break;
    }

    /// <summary>
    /// UnityEditor.EditorUserBuildSettings.activeBuildTarget, Can Run in any platform~
    /// </summary>
    public static string UnityEditor_EditorUserBuildSettings_activeBuildTarget
    {
        get
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in assemblies)
            {
                if (a.GetName().Name == "UnityEditor")
                {
                    Type lockType = a.GetType("UnityEditor.EditorUserBuildSettings");
                    //var retObj = lockType.GetMethod(staticMethodName,
                    //    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    //    .Invoke(null, args);
                    //return retObj;
                    var p = lockType.GetProperty("activeBuildTarget");

                    var em = p.GetGetMethod().Invoke(null, new object[] { }).ToString();
                    return em;
                }

            }
            return null;
        }
    }
    /// <summary>
    /// Different platform's assetBundles is incompatible. 
    /// CosmosEngine put different platform's assetBundles in different folder.
    /// Here, get Platform name that represent the AssetBundles Folder.
    /// </summary>
    /// <returns>Platform folder Name</returns>
    public static string GetBuildPlatformName()
    {
        string buildPlatformName = "Win32"; // default
        if (Application.isEditor)
        {
            // 根据编辑器的当前编译环境, 来确定读取哪个资源目录
            // 因为美术库是根据编译环境来编译资源的，这样可以在Unity编辑器上， 快速验证其资源是否正确再放到手机上
            var buildTarget = UnityEditor_EditorUserBuildSettings_activeBuildTarget;
            switch (buildTarget)
            {
                case "StandaloneWindows":
                case "StandaloneWindows64":
                    buildPlatformName = "Win32";
                    break;
                case "Android":
                    buildPlatformName = "Android";
                    break;
                case "iPhone":
                    buildPlatformName = "IOS";
                    break;
                default:
                    CDebug.Assert(false);
                    break;
            }
        }
        else
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    buildPlatformName = "Android";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    buildPlatformName = "IOS";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsWebPlayer:
                    buildPlatformName = "Win32";
                    break;
                default:
                    CDebug.Assert(false);
                    break;
            }
        }

        return buildPlatformName;
    }

    /// <summary>
    /// On Windows, file protocol has a strange rule that has one more slash
    /// </summary>
    /// <returns>string, file protocol string</returns>
    public static string GetFileProtocol()
    {
        string fileProtocol = "file://";
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsWebPlayer)
            fileProtocol = "file:///";

        return fileProtocol;
    }

    /// <summary>
    /// Initialize the path of AssetBundles store place ( Maybe in PersitentDataPath or StreamingAssetsPath )
    /// </summary>
    /// <returns></returns>
    public static void InitResourcePath()
    {
        string productPath = ProductFullPath;
        string assetBundlePath = Path.Combine(Application.dataPath, CCosmosEngine.GetConfig("AssetBundleRelPath"));
        string resourceDirName = Path.GetFileName(CCosmosEngine.GetConfig("AssetBundleRelPath"));

        DocumentResourcesPathWithoutFileProtocol = string.Format("{0}/{1}/{2}/", GetAppDataPath(), resourceDirName, GetBuildPlatformName());  // 各平台通用
        DocumentResourcesPath = FileProtocol + DocumentResourcesPathWithoutFileProtocol;

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXEditor:
                {
                    ApplicationPath = string.Format("{0}{1}/", GetFileProtocol(), productPath);
                    ResourcesPath = GetFileProtocol() + assetBundlePath + "/" + BuildPlatformName + "/";
                    ResourcesPathWithoutFileProtocol = assetBundlePath + "/" + BuildPlatformName + "/";

                }
                break;
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXPlayer:
                {
                    string path = Application.dataPath.Replace('\\', '/');
                    path = path.Substring(0, path.LastIndexOf('/') + 1);
                    ApplicationPath = string.Format("{0}{1}/", GetFileProtocol(), path);
                    ResourcesPath = string.Format("{0}{1}{2}/{3}/", GetFileProtocol(), path, resourceDirName, GetBuildPlatformName());
                    ResourcesPathWithoutFileProtocol = string.Format("{0}{1}/{2}/", path, resourceDirName, GetBuildPlatformName());

                }
                break;
            case RuntimePlatform.Android:
                {
                    ApplicationPath = string.Concat("jar:", GetFileProtocol(), Application.dataPath, "!/assets/");

                    ResourcesPath = string.Concat(ApplicationPath, GetBuildPlatformName(), "/");
                    ResourcesPathWithoutFileProtocol = string.Concat(Application.dataPath, "!/assets/", GetBuildPlatformName() + "/");  // 注意，StramingAsset在Android平台中，是在壓縮的apk里，不做文件檢查
                }
                break;
            case RuntimePlatform.IPhonePlayer:
                {
                    ApplicationPath = System.Uri.EscapeUriString(GetFileProtocol() + Application.streamingAssetsPath + "/");  // MacOSX下，带空格的文件夹，空格字符需要转义成%20
                    ResourcesPath = string.Format("{0}{1}/", ApplicationPath, GetBuildPlatformName());  // only iPhone need to Escape the fucking Url!!! other platform works without it!!! Keng Die!
                    ResourcesPathWithoutFileProtocol = Application.streamingAssetsPath + "/" + GetBuildPlatformName() + "/";
                }
                break;
            default:
                {
                    CDebug.Assert(false);
                }
                break;
        }

        if (Debug.isDebugBuild)
        {
            CDebug.Log("ResourceManager ApplicationPath: {0}", ApplicationPath);
            CDebug.Log("ResourceManager ResourcesPath: {0}", ResourcesPath);
            CDebug.Log("ResourceManager DocumentResourcesPath: {0}", DocumentResourcesPath);
            CDebug.Log("================================================================================");
        }
    }

    public static void LogRequest(string resType, string resPath)
    {
        if (LogLevel < (int)LoadingLogLevel.ShowDetail)
            return;

        CDebug.Log("[Request] {0}, {1}", resType, resPath);
    }

    public static void LogLoadTime(string resType, string resPath, System.DateTime begin)
    {
        if (LogLevel < (int)LoadingLogLevel.ShowTime)
            return;

        CDebug.Log("[Load] {0}, {1}, {2}s", resType, resPath, (System.DateTime.Now - begin).TotalSeconds);
    }

    public static void Collect()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
