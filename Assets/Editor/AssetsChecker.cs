using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

public class MaterialDetails
{	
    public Material material;
	public List<GameObject> FoundInGameObjects = new List<GameObject>();		
};

public class AssetsChecker : EditorWindow
{

    static int MinWidth = 480;
    string inputPath = "";
    Color defColor;

    Vector2 textureListScrollPos=new Vector2(0,0);
    Vector2 materialListScrollPos=new Vector2(0,0);

    
	List<MaterialDetails> AllMaterials = new List<MaterialDetails>();
	

    enum InspectType 
	{
		Textures, Materials, Meshes, Shaders, Sounds, Scripts
	};

    InspectType ActiveInspectType=InspectType.Materials;

    [MenuItem("Window/Assets Checker")]
    static void Init()
    {
        AssetsChecker window = (AssetsChecker)EditorWindow.GetWindow(typeof(AssetsChecker));
        window.checkResources();
        window.minSize = new Vector2(MinWidth, 475);
    }

    void OnGUI()
    {
        defColor = GUI.color;
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
     
        inputPath = EditorGUILayout.TextField((inputPath), GUILayout.Width(400), GUILayout.Height(30));
        if(GUILayout.Button(("Check"), GUILayout.Width(50), GUILayout.Height(30)))
            checkResources();
        GUILayout.EndHorizontal();
        
        GUILayout.Space(20);


        Texture2D iconTexture = AssetPreview.GetMiniTypeThumbnail( typeof( Texture2D ) );
		Texture2D iconMaterial = AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
		Texture2D iconMesh = AssetPreview.GetMiniTypeThumbnail( typeof( Mesh ) );
		Texture2D iconShader = AssetPreview.GetMiniTypeThumbnail( typeof( Shader ) );
		Texture2D iconSound = AssetPreview.GetMiniTypeThumbnail( typeof( AudioClip ) );
		Texture2D iconScript = AssetPreview.GetMiniTypeThumbnail( typeof( MonoScript ) );
       

        GUIContent [] guiObjs = 
		{
			new GUIContent( iconTexture, "Active Textures" ), 
			new GUIContent( iconMaterial, "Active Materials" ), 
			new GUIContent( iconMesh, "Active Meshes" ), 
			new GUIContent( iconShader, "Active Shaders" ), 
			new GUIContent( iconSound, "Active Sounds" ),
			new GUIContent( iconScript, "Active Scripts" ),
		};

        GUILayoutOption [] options = 
		{
			GUILayout.Width( 300 ),
			GUILayout.Height( 50 ),
		};
		
		GUILayout.BeginHorizontal();
		//GUILayout.FlexibleSpace();
		ActiveInspectType=(InspectType)GUILayout.Toolbar((int)ActiveInspectType,guiObjs,options);
		GUILayout.Box((
			"Summary\n\n" +
			"Materials: " + AllMaterials.Count ), GUILayout.Width(150), GUILayout.Height(50));
        //GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
        GUILayout.Space(20);

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        switch (ActiveInspectType)
		{
		/*
        case InspectType.Textures:
			ListTextures();
			break;
        */
		case InspectType.Materials:
			ListMaterials();
			break;
        /* 
		case InspectType.Meshes:
			ListMeshes();
			break;
		case InspectType.Shaders:
			ListShaders();
			break;
		case InspectType.Sounds:
			ListSounds();
			break;
		case InspectType.Scripts:
			ListScripts();
			break;
        */
		}

       
    }

    //读取相应的List，并打印到屏幕上
    void ListMaterials()
	{
        //GUIStyle gs = new GUIStyle();
        materialListScrollPos = EditorGUILayout.BeginScrollView(materialListScrollPos);

       
        
        foreach (MaterialDetails mat in AllMaterials)
        {
            GUILayout.BeginHorizontal();
            //Texture thumb = mat.material
            //AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
            GUILayout.Box(AssetPreview.GetAssetPreview(mat.material), GUILayout.Width(50), GUILayout.Height(50));
            //GUILayout.Box( thumb, GUILayout.Width(50), GUILayout.Height(50) );
            GUILayout.Button( new GUIContent( mat.material.name, mat.material.name), GUILayout.Width(150), GUILayout.Height(50) );
            GUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }

    //将资源分类，并存到对应的list里
    void checkResources()
    {
        AllMaterials.Clear();
    
        Material [] materials = GetAtPath<Material>(inputPath);

        foreach (Material material in materials){
            MaterialDetails tMaterialDetails = new MaterialDetails();
            tMaterialDetails.material = material;
            AllMaterials.Add(tMaterialDetails);
        }
        
    }

    //找到目录下的指定类型文件，返回加载后的object
    private T[] GetAtPath<T>(string path)
    {
        ArrayList al = new ArrayList();
        string[] fileEntries = Directory.GetFiles(Application.dataPath + path);
      
        foreach (string fileName in fileEntries)
        {

            string temp = fileName.Replace("\\", "/");
            
            int index = temp.LastIndexOf("/");
            string localPath = "Assets" + path;
 
            if (index > 0)
                localPath += temp.Substring(index);


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

