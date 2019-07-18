
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

public class AssetsChecker : EditorWindow
{

    static int MinWidth = 480;
    string inputPath = "";
    Color defColor;

    [MenuItem("Window/Assets Checker")]
    static void Init()
    {
        AssetsChecker window = (AssetsChecker)EditorWindow.GetWindow(typeof(AssetsChecker));

        window.minSize = new Vector2(MinWidth, 475);
    }

    void OnGUI()
    {
        defColor = GUI.color;
        Texture2D iconMaterials = AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
        if (GUILayout.Button("Click" ,GUILayout.Width(80), GUILayout.Height(80)))
            checkResources();
       
        GUILayout.Space(10);
        inputPath = EditorGUILayout.TextField("Input the resources path ", inputPath);
        GUILayout.Space(10);
        
    }


    void checkResources()
    {
        Renderer [] renderers = GetAtPath<Renderer>(inputPath);
        //Graphic [] renderers = GetAtPath<Graphic>(inputPath);
        foreach (Renderer renderer in renderers){
            Debug.Log(renderer);
        }
        /*string[] guids1 = AssetDatabase.FindAssets(inputPath);
        foreach (string guid1 in guids1)
        {
            Debug.Log(AssetDatabase.GUIDToAssetPath(guid1));
        }
        */
        
    }

    //找到目录下的全部文件，并返回加载后的object
    private T[] GetAtPath<T>(string path)
    {
        ArrayList al = new ArrayList();
        string[] fileEntries = Directory.GetFiles(Application.dataPath + path);
      
        foreach (string fileName in fileEntries)
        {
            Debug.Log(fileName);
            string temp = fileName.Replace("\\", "/");
            
            int index = temp.LastIndexOf("/");
            string localPath = "Assets" + path;
 
            if (index > 0)
                localPath += temp.Substring(index);
            Debug.Log(localPath);

            //这行有点问题，路径对了但是没有加载
            Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));
           
            if (t != null)
                al.Add(t);
        }
 
        T[] result = new T[al.Count];
 
        for (int i = 0; i < al.Count; i++){
            result[i] = (T) al[i];
            
        }
        return result;
    }
}
