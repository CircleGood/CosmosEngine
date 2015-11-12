﻿//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class CCommonProductPrefabExporter : CBuild_Base
{
    public override string GetDirectory() { return ""; }
    public override string GetExtention() { return "dir"; }

    public override void Export(string path)
    {
        path = path.Replace('\\', '/');

        string[] fileArray = Directory.GetFiles(path, "*.prefab");
        foreach (string file in fileArray)
        {
            string filePath = file.Replace('\\', '/');
            CDebug.Log("Build Func To: " + filePath);
        }
    }

    [MenuItem("CosmosEngine/Build Product Folder Prefabs")]
    static void BuildProductFolderPrefabs()
    {
        CAutoResourceBuilder.ProductExport(new CCommonProductPrefabExporter());
    }
}
