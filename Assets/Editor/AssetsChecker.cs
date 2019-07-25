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

public class TextureDetails
{	
    public Texture texture;
    public int memSizeBytes = 0;

    public List<Object> FoundInMaterials=new List<Object>();
	public List<GameObject> FoundInGameObjects = new List<GameObject>();	

    public static int CalculateTextureSizeBytes(Texture tTexture)
	{
		int tWidth=tTexture.width;
		int tHeight=tTexture.height;
		if (tTexture is Texture2D)
		{
			Texture2D tTex2D=tTexture as Texture2D;
		 	int bitsPerPixel=GetBitsPerPixel(tTex2D.format);
			int mipMapCount=tTex2D.mipmapCount;
			int mipLevel=1;
			int tSize=0;
			while (mipLevel<=mipMapCount)
			{
				tSize+=tWidth*tHeight*bitsPerPixel/8;
				tWidth=tWidth/2;
				tHeight=tHeight/2;
				mipLevel++;
			}
			return tSize;
		}
		
		if (tTexture is Cubemap)
		{
			Cubemap tCubemap=tTexture as Cubemap;
		 	int bitsPerPixel=GetBitsPerPixel(tCubemap.format);
			return tWidth*tHeight*6*bitsPerPixel/8;
		}
		return 0;
	}

    public static int GetBitsPerPixel(TextureFormat format)
	{
		switch (format)
		{
		case TextureFormat.Alpha8: //	 Alpha-only texture format.
			return 8;
		case TextureFormat.ARGB4444: //	 A 16 bits/pixel texture format. Texture stores color with an alpha channel.
			return 16;
		case TextureFormat.RGBA4444: //	 A 16 bits/pixel texture format.
			return 16;
		case TextureFormat.RGB24:	// A color texture format.
			return 24;
		case TextureFormat.RGBA32:	//Color with an alpha channel texture format.
			return 32;
		case TextureFormat.ARGB32:	//Color with an alpha channel texture format.
			return 32;
		case TextureFormat.RGB565:	//	 A 16 bit color texture format.
			return 16;
		case TextureFormat.DXT1:	// Compressed color texture format.
			return 4;
		case TextureFormat.DXT5:	// Compressed color with alpha channel texture format.
			return 8;
		case TextureFormat.PVRTC_RGB2://	 PowerVR (iOS) 2 bits/pixel compressed color texture format.
			return 2;
		case TextureFormat.PVRTC_RGBA2://	 PowerVR (iOS) 2 bits/pixel compressed with alpha channel texture format
			return 2;
		case TextureFormat.PVRTC_RGB4://	 PowerVR (iOS) 4 bits/pixel compressed color texture format.
			return 4;
		case TextureFormat.PVRTC_RGBA4://	 PowerVR (iOS) 4 bits/pixel compressed with alpha channel texture format
			return 4;
		case TextureFormat.ETC_RGB4://	 ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
			return 4;								
		case TextureFormat.BGRA32://	 Format returned by iPhone camera
			return 32;
			#if !UNITY_5 && !UNITY_5_3_OR_NEWER
			case TextureFormat.ATF_RGB_DXT1://	 Flash-specific RGB DXT1 compressed color texture format.
			case TextureFormat.ATF_RGBA_JPG://	 Flash-specific RGBA JPG-compressed color texture format.
			case TextureFormat.ATF_RGB_JPG://	 Flash-specific RGB JPG-compressed color texture format.
			return 0; //Not supported yet  
			#endif
		}
		return 0;
	}
};

public class AssetsChecker : EditorWindow
{

    static int MinWidth = 480;
    string inputPath = "";

    //默认按字母顺序排序
    int sortType = 1;
    int TotalTextureMemory = 0;
    Color defColor;

    Vector2 textureListScrollPos=new Vector2(0,0);
    Vector2 materialListScrollPos=new Vector2(0,0);

    
	List<MaterialDetails> AllMaterials = new List<MaterialDetails>();
	List<TextureDetails> AllTextures = new List<TextureDetails>();

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
        Texture2D iconTexture = AssetPreview.GetMiniTypeThumbnail( typeof( Texture2D ) );
		Texture2D iconMaterial = AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
		Texture2D iconMesh = AssetPreview.GetMiniTypeThumbnail( typeof( Mesh ) );
		Texture2D iconShader = AssetPreview.GetMiniTypeThumbnail( typeof( Shader ) );
		Texture2D iconSound = AssetPreview.GetMiniTypeThumbnail( typeof( AudioClip ) );
		Texture2D iconScript = AssetPreview.GetMiniTypeThumbnail( typeof( MonoScript ) );
        Texture2D iconSortAlpha = EditorGUIUtility.FindTexture("AlphabeticalSorting");
        Texture2D iconSortDefault = EditorGUIUtility.FindTexture("DefaultSorting");
        Texture2D iconSortDepend = EditorGUIUtility.FindTexture("CustomSorting");
        Texture2D iconFolder = EditorGUIUtility.FindTexture("Folder Icon");
        Texture2D iconRefresh = EditorGUIUtility.FindTexture("vcs_Refresh");

        GUILayout.Space(15);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.Box(iconFolder,GUILayout.Width(30), GUILayout.Height(30));
        inputPath = EditorGUILayout.TextField(inputPath, GUILayout.Width(385), GUILayout.Height(30));
        if(GUILayout.Button(iconRefresh, GUILayout.Width(30), GUILayout.Height(30)))
            checkResources();
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);


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
        GUILayout.Space(19);
		//GUILayout.FlexibleSpace();
		ActiveInspectType=(InspectType)GUILayout.Toolbar((int)ActiveInspectType,guiObjs,options);

      

		GUILayout.Box((
			"Summary\n" +
			"Materials: " + AllMaterials.Count + "\n" + 
            "Textures: " + AllTextures.Count + " - " + EditorUtility.FormatBytes(TotalTextureMemory)), 
            GUILayout.Width(150), GUILayout.Height(100));
        //GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        if(GUILayout.Button(iconSortDefault, GUILayout.Width(30), GUILayout.Height(30))){
            sortType = 1;
            checkResources();
        }
        
        if(GUILayout.Button(iconSortAlpha, GUILayout.Width(30), GUILayout.Height(30))){
            sortType = 2;
            checkResources();
        }
        
        if(GUILayout.Button(iconSortDepend, GUILayout.Width(30), GUILayout.Height(30))){
            sortType = 3;
            checkResources();
        }
		GUILayout.EndHorizontal();
        

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        switch (ActiveInspectType)
		{
		
        case InspectType.Textures:
			ListTextures();
			break;
        
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

       
        
        foreach (MaterialDetails mat in AllMaterials){
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            //Texture thumb = mat.material
            //AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
            GUILayout.Box(AssetPreview.GetAssetPreview(mat.material), GUILayout.Width(50), GUILayout.Height(50));
            //GUILayout.Box( thumb, GUILayout.Width(50), GUILayout.Height(50) );
            GUILayout.Button( new GUIContent( mat.material.name, mat.material.name), GUILayout.Width(150), GUILayout.Height(50) );
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }
        
        EditorGUILayout.EndScrollView();
    }

    void ListTextures(){

        textureListScrollPos = EditorGUILayout.BeginScrollView(textureListScrollPos);

        foreach (TextureDetails tex in AllTextures){
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            //Texture thumb = mat.material
            //AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
            GUILayout.Box(AssetPreview.GetAssetPreview(tex.texture), GUILayout.Width(50), GUILayout.Height(50));
            //GUILayout.Box( thumb, GUILayout.Width(50), GUILayout.Height(50) );
            GUILayout.Button( new GUIContent( tex.texture.name, tex.texture.name), GUILayout.Width(150), GUILayout.Height(50) );
            Texture2D iconMaterials = AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
			GUILayout.Button( new GUIContent( tex.FoundInMaterials.Count.ToString(), iconMaterials, "Materials" ), GUILayout.Width(60), GUILayout.Height(50));
			GUILayout.Box("\n" + EditorUtility.FormatBytes(tex.memSizeBytes),GUILayout.Width(120),GUILayout.Height(50));
            GUILayout.EndHorizontal(); 
            GUILayout.Space(5);
        }
        EditorGUILayout.EndScrollView();
    }


    //将资源分类，并存到对应的list里
    void checkResources()
    {
        AllMaterials.Clear();
        AllTextures.Clear();
        TotalTextureMemory = 0;

        Material [] materials = GetAtPath<Material>(inputPath);

        foreach (Material material in materials){
            MaterialDetails tMaterialDetails = new MaterialDetails();
            tMaterialDetails.material = material;
            AllMaterials.Add(tMaterialDetails);
        }
        
        //找到material使用的texture
        foreach (MaterialDetails tMaterialDetails in AllMaterials){
            Material tMaterial = tMaterialDetails.material;
            foreach (Object obj in EditorUtility.CollectDependencies(new UnityEngine.Object[] {tMaterial})){
                if (obj is Texture)
				{
					Texture tTexture = obj as Texture;
					if(tTexture != null){
                        int check = 0;
                       
                        foreach (TextureDetails details in AllTextures){
			                if (details.texture==tTexture) {
                                check = 1;
                                details.FoundInMaterials.Add(tMaterial);
                            }
		                }
                        if(check == 0){
                            TextureDetails tTextureDetails = new TextureDetails();
                            tTextureDetails.texture = tTexture;
                            AllTextures.Add(tTextureDetails);
                            tTextureDetails.FoundInMaterials.Add(tMaterial);
                        }
                       
                    }
					
				}
            }
        }
        
        
        foreach(TextureDetails tTextureDetails in AllTextures){
            tTextureDetails.memSizeBytes = TextureDetails.CalculateTextureSizeBytes(tTextureDetails.texture);
            TotalTextureMemory += tTextureDetails.memSizeBytes;
        }

        //按占内存多少排序
        if(sortType == 1){
            AllTextures.Sort(delegate(TextureDetails details1, TextureDetails details2) {return details2.memSizeBytes-details1.memSizeBytes;});
        }
        //按引用顺序排序
        if(sortType == 3){
            AllTextures.Sort(delegate(TextureDetails details1, TextureDetails details2) {return details2.FoundInMaterials.Count-details1.FoundInMaterials.Count;});
        }
    }

    //找到目录下的指定类型文件，返回加载后的object
    private T[] GetAtPath<T>(string path)
    {
        ArrayList al = new ArrayList();
        int dash = path.IndexOf("/");
        
        string shortPath;
        if(path.Equals("Assets")){
            shortPath = "";
        }
        else shortPath = path.Substring(dash);
        //Debug.Log(shortPath);
        
        //string shortPath = path;
        string[] fileEntries = Directory.GetFiles(Application.dataPath + shortPath);
      
        foreach (string fileName in fileEntries)
        {

            string temp = fileName.Replace("\\", "/");
            
            int index = temp.LastIndexOf("/");
            //string localPath = "Assets" + path;
            string localPath = path;
            if (index > 0)
                localPath += temp.Substring(index);

            //Debug.Log(localPath);
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
