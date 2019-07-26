using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

public class MaterialDetails{	
    public Material material;
	public List<GameObject> FoundInGameObjects = new List<GameObject>();		
};

public class TextureDetails{	
    public Texture texture;
    public int memSizeBytes = 0;

    public List<Object> FoundInMaterials=new List<Object>();
	public List<GameObject> FoundInGameObjects = new List<GameObject>();	

    public static int CalculateTextureSizeBytes(Texture tTexture){
		int tWidth=tTexture.width;
		int tHeight=tTexture.height;
		if (tTexture is Texture2D){
			Texture2D tTex2D=tTexture as Texture2D;
		 	int bitsPerPixel=GetBitsPerPixel(tTex2D.format);
			int mipMapCount=tTex2D.mipmapCount;
			int mipLevel=1;
			int tSize=0;
			while (mipLevel<=mipMapCount){
				tSize+=tWidth*tHeight*bitsPerPixel/8;
				tWidth=tWidth/2;
				tHeight=tHeight/2;
				mipLevel++;
			}
			return tSize;
		}
		
		if (tTexture is Cubemap){
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

        	case TextureFormat.BC7:
            		return 8;

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

public class MeshDetails{
    public Mesh mesh;
    public List<GameObject> FoundInGameObjects = new List<GameObject>();
};

public class ShaderDetails{	
    public Shader shader;
	public List<GameObject> FoundInGameObjects = new List<GameObject>();
    public List<Material> FoundInMaterials = new List<Material>();		
};

public class SoundDetails{
    public AudioClip clip;
	public List<GameObject> FoundInGameObjects = new List<GameObject>(); 
};

public class ScriptDetails{
    public MonoScript script;
	public List<GameObject> FoundInGameObjects = new List<GameObject>();
};

public class AssetsChecker : EditorWindow{

    static int MinWidth = 480;
    int TotalTextureMemory = 0;
    int TotalMeshVertices = 0;
    int checkOption = 1;

    string inputPath = "Assets";
    ArrayList inputPathList = new ArrayList();

    Color defColor;

    Vector2 textureListScrollPos=new Vector2(0,0);
    Vector2 materialListScrollPos=new Vector2(0,0);
    Vector2 meshListScrollPos=new Vector2(0,0);
    Vector2 shaderListScrollPos=new Vector2(0,0);
	Vector2 soundListScrollPos=new Vector2(0,0);
	Vector2 scriptListScrollPos=new Vector2(0,0);

	List<MaterialDetails> AllMaterials = new List<MaterialDetails>();
	List<TextureDetails> AllTextures = new List<TextureDetails>();
    List<MeshDetails> AllMeshes = new List<MeshDetails>();
    List<ShaderDetails> AllShaders = new List<ShaderDetails>();
	List<SoundDetails> AllSounds = new List<SoundDetails>();
	List<ScriptDetails> AllScripts = new List<ScriptDetails>();

    enum InspectType 
    {
		Textures, Materials, Meshes, Shaders, Sounds, Scripts
	};

    InspectType ActiveInspectType=InspectType.Materials;

    [MenuItem("Window/Assets Checker")]
    static void Init()
    {
        AssetsChecker window = (AssetsChecker)EditorWindow.GetWindow(typeof(AssetsChecker));
        window.minSize = new Vector2(MinWidth, 475);
    }

    void OnGUI(){
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
        Texture2D iconPlus = EditorGUIUtility.FindTexture("Toolbar Plus");

        GUILayout.Space(15);

        GUIContent [] guiObjs = 
		{
			new GUIContent( iconTexture, "Textures" ), 
			new GUIContent( iconMaterial, "Materials" ), 
			new GUIContent( iconMesh, "Meshes" ), 
			new GUIContent( iconShader, "Shaders" ), 
			new GUIContent( iconSound, "Sounds" ),
			new GUIContent( iconScript, "Scripts" ),
		};

        GUILayoutOption [] options = 
		{
			GUILayout.Width( 300 ),
			GUILayout.Height( 50 ),
		};	

		GUILayout.BeginHorizontal();
        GUILayout.Space(19);

		ActiveInspectType=(InspectType)GUILayout.Toolbar((int)ActiveInspectType,guiObjs,options);    

		GUILayout.Box((
			"Summary\n" +
			"Materials: " + AllMaterials.Count + "\n" + 
            "Textures: " + AllTextures.Count + " - " + EditorUtility.FormatBytes(TotalTextureMemory)) + "\n" +
            "Meshes " + AllMeshes.Count  + " - "  + TotalMeshVertices.ToString()  + "\n" + 
            "Shaders: " + AllShaders.Count + "\n" +
            "Sounds: " + AllSounds.Count + "\n" + 
            "Scripts: " + AllScripts.Count  + "\n",
            GUILayout.Width(150), GUILayout.Height(100));

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);

        GUILayout.Box(iconFolder,GUILayout.Width(30), GUILayout.Height(30));
        inputPath = EditorGUILayout.TextField(inputPath, GUILayout.Width(350), GUILayout.Height(30));
        if(GUILayout.Button(new GUIContent(iconPlus, "Add to list"),  GUILayout.Width(30), GUILayout.Height(30))){
            int check = 0;
            if(checkOption == 1){
                foreach(string s in inputPathList){
                    if (s.Equals(inputPath)){
                        check = 1;
                        break;
                    }
                }
                if(check == 0){
                    checkResources();
                    inputPathList.Add(inputPath);
                }
            }
        }
        if(GUILayout.Button(new GUIContent(iconRefresh, "Clear list"), GUILayout.Width(30), GUILayout.Height(30))){
            clearResources();
            checkOption = 1;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        if(GUILayout.Button(new GUIContent(iconSortDefault, "Sort by size"), GUILayout.Width(30), GUILayout.Height(30))){
            AllTextures.Sort(delegate(TextureDetails details1, TextureDetails details2) {return details2.memSizeBytes-details1.memSizeBytes;});
            AllMeshes.Sort(delegate(MeshDetails details1, MeshDetails details2) {return details2.mesh.vertexCount-details1.mesh.vertexCount;});
        }
        
        if(GUILayout.Button(new GUIContent(iconSortAlpha, "Sort Alphabetically"), GUILayout.Width(30), GUILayout.Height(30))){
            AllTextures.Sort(delegate(TextureDetails details1, TextureDetails details2) {return string.Compare(details1.texture.name,details2.texture.name);});
            AllMaterials.Sort(delegate(MaterialDetails details1, MaterialDetails details2) {return string.Compare(details1.material.name,details2.material.name);});
            AllMeshes.Sort(delegate(MeshDetails details1, MeshDetails details2) {return string.Compare(details1.mesh.name,details2.mesh.name);});
            AllShaders.Sort(delegate(ShaderDetails details1, ShaderDetails details2) {return string.Compare(details1.shader.name,details2.shader.name);});
            AllSounds.Sort(delegate(SoundDetails details1, SoundDetails details2) {return string.Compare(details1.clip.name,details2.clip.name);});
            AllScripts.Sort(delegate(ScriptDetails details1, ScriptDetails details2) {return string.Compare(details1.script.name,details2.script.name);});
        }
        
        if(GUILayout.Button(new GUIContent(iconSortDepend, "Sort by Dependency"), GUILayout.Width(30), GUILayout.Height(30))){
            AllTextures.Sort(delegate(TextureDetails details1, TextureDetails details2) {return details2.FoundInMaterials.Count-details1.FoundInMaterials.Count;});
            AllShaders.Sort(delegate(ShaderDetails details1, ShaderDetails details2) {return details2.FoundInMaterials.Count-details1.FoundInMaterials.Count;});
        }

        GUILayout.Space(250);

        if(GUILayout.Button("Check all assets", GUILayout.Width(100), GUILayout.Height(30))){
            if(checkOption != 2){
                checkOption = 2;
                checkResources();
            }
        }
		GUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        switch (ActiveInspectType){
		
        case InspectType.Textures:
			ListTextures();
			break;
        
		case InspectType.Materials:
			ListMaterials();
			break;
        
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
        
		}  
    }

    //读取相应的List，并打印到屏幕上
    void ListMaterials(){
	
        materialListScrollPos = EditorGUILayout.BeginScrollView(materialListScrollPos);     
        
        foreach (MaterialDetails mat in AllMaterials){
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.Box(AssetPreview.GetAssetPreview(mat.material), GUILayout.Width(50), GUILayout.Height(50));
            if(GUILayout.Button( new GUIContent( mat.material.name, mat.material.name), GUILayout.Width(150), GUILayout.Height(50))){
                Selection.activeObject = mat.material;
            }
            GUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }

    void ListTextures(){

        textureListScrollPos = EditorGUILayout.BeginScrollView(textureListScrollPos);

        foreach (TextureDetails tex in AllTextures){
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.Box(AssetPreview.GetAssetPreview(tex.texture), GUILayout.Width(50), GUILayout.Height(50));
            if(GUILayout.Button( new GUIContent( tex.texture.name, tex.texture.name), GUILayout.Width(150), GUILayout.Height(50) )){
                Selection.activeObject = tex.texture;
            }
            Texture2D iconMaterials = AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
			if(GUILayout.Button( new GUIContent( tex.FoundInMaterials.Count.ToString(), iconMaterials, "Materials" ), GUILayout.Width(60), GUILayout.Height(50))){
                Selection.objects = tex.FoundInMaterials.ToArray();
            }
			GUILayout.Box(tex.texture.width.ToString() + " x " + tex.texture.height.ToString() + "\n" +
            EditorUtility.FormatBytes(tex.memSizeBytes) + "\n" 
         
            ,GUILayout.Width(120),GUILayout.Height(50));

            GUILayout.EndHorizontal(); 
        }
        EditorGUILayout.EndScrollView();
    }

    void ListMeshes(){
        meshListScrollPos = EditorGUILayout.BeginScrollView(meshListScrollPos);

        foreach (MeshDetails mes in AllMeshes){
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.Box(AssetPreview.GetAssetPreview(mes.mesh), GUILayout.Width(50), GUILayout.Height(50));
            if(GUILayout.Button( new GUIContent( mes.mesh.name, mes.mesh.name), GUILayout.Width(150), GUILayout.Height(50))){
                Selection.activeObject = mes.mesh;
            }
            GUILayout.Box( mes.mesh.vertexCount.ToString() + " vertices\n" + mes.mesh.triangles.Length + " Traingles\n", GUILayout.Width(100),GUILayout.Height(50));

            GUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }

    void ListShaders(){

        shaderListScrollPos = EditorGUILayout.BeginScrollView(shaderListScrollPos);

        foreach (ShaderDetails sdr in AllShaders){
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            Texture2D iconShader = AssetPreview.GetMiniTypeThumbnail( typeof( Shader ) );
            GUILayout.Box(iconShader, GUILayout.Width(50), GUILayout.Height(50));
            if(GUILayout.Button( new GUIContent( sdr.shader.name, sdr.shader.name), GUILayout.Width(150), GUILayout.Height(50) )){
                Selection.activeObject = sdr.shader;
            }
            Texture2D iconMaterials = AssetPreview.GetMiniTypeThumbnail( typeof( Material ) );
			if(GUILayout.Button( new GUIContent( sdr.FoundInMaterials.Count.ToString(), iconMaterials, "Materials" ), GUILayout.Width(60), GUILayout.Height(50))){
                Selection.objects = sdr.FoundInMaterials.ToArray();
            }

            GUILayout.EndHorizontal(); 
        }
        EditorGUILayout.EndScrollView();
    }

   void ListSounds(){
        soundListScrollPos = EditorGUILayout.BeginScrollView(soundListScrollPos);

        foreach (SoundDetails snd in AllSounds){
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.Box(AssetPreview.GetAssetPreview(snd.clip), GUILayout.Width(50), GUILayout.Height(50));
            if(GUILayout.Button( new GUIContent( snd.clip.name, snd.clip.name), GUILayout.Width(150), GUILayout.Height(50))){
                Selection.activeObject = snd.clip;
            }
            GUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }

    void ListScripts(){
        scriptListScrollPos = EditorGUILayout.BeginScrollView(scriptListScrollPos);

        foreach(ScriptDetails scp in AllScripts){
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            Texture2D iconScript = AssetPreview.GetMiniTypeThumbnail( typeof( MonoScript ) );
            GUILayout.Box(iconScript, GUILayout.Width(50), GUILayout.Height(50));
            if(GUILayout.Button( new GUIContent( scp.script.name, scp.script.name), GUILayout.Width(150), GUILayout.Height(50))){
                Selection.activeObject = scp.script;
            }
            GUILayout.EndHorizontal();            
        }
        EditorGUILayout.EndScrollView();
    }

    void clearResources(){
        AllMaterials.Clear();
        AllTextures.Clear();
        AllMeshes.Clear();
        AllShaders.Clear();
        AllSounds.Clear();
        AllScripts.Clear();
        inputPathList.Clear();
        TotalTextureMemory = 0;
        TotalMeshVertices = 0;
    }

    void checkResources()
    {
        Material [] materials = {};
        if(checkOption == 1){
            materials = GetAtPath<Material>(inputPath);
        }
        if(checkOption == 2){
            materials = GetAllFiles<Material>(inputPath);
        }

        foreach (Material material in materials){
            MaterialDetails tMaterialDetails = new MaterialDetails();
            tMaterialDetails.material = material;
            AllMaterials.Add(tMaterialDetails);
        }
        
        //找到material使用的texture和shader
        foreach (MaterialDetails tMaterialDetails in AllMaterials){
            Material tMaterial = tMaterialDetails.material;
            foreach (Object obj in EditorUtility.CollectDependencies(new UnityEngine.Object[] {tMaterial})){
                if(obj is Texture){
					Texture tTexture = obj as Texture;
					if(tTexture != null){
                        int check = 0;                      
                        foreach(TextureDetails details in AllTextures){
			                if (details.texture==tTexture){
                                check = 1;
                                details.FoundInMaterials.Add(tMaterial);
                                break;
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
                if(obj is Shader){
                    Shader tShader = obj as Shader;
                    if(tShader != null){
                        int check = 0;
                        foreach(ShaderDetails details in AllShaders){
                            if(details.shader==tShader){
                                check = 1;
                                details.FoundInMaterials.Add(tMaterial);
                                break;
                            }
                        }
                        if(check == 0){
                            ShaderDetails tShaderDetails = new ShaderDetails();
                            tShaderDetails.shader = tShader;
                            AllShaders.Add(tShaderDetails);
                            tShaderDetails.FoundInMaterials.Add(tMaterial);
                        }              
                    }
                }
            }
        }

        Mesh[] meshes = {};
        if(checkOption == 1){
            meshes = GetAtPath<Mesh>(inputPath);
        }
        if(checkOption == 2){
            meshes = GetAllFiles<Mesh>(inputPath);
        }

        foreach (Mesh mesh in meshes){
            MeshDetails tMeshDetails = new MeshDetails();
            tMeshDetails.mesh = mesh;
            AllMeshes.Add(tMeshDetails);
        }
        
        AudioClip[] clips = {};
        if(checkOption == 1){
            clips = GetAtPath<AudioClip>(inputPath);
        }
        if(checkOption == 2){
            clips = GetAllFiles<AudioClip>(inputPath);
        }

        foreach (AudioClip clip in clips){
            SoundDetails tSoundDetails = new SoundDetails();
            tSoundDetails.clip = clip;
            AllSounds.Add(tSoundDetails);
        }

        MonoScript[] scripts = {};
        if(checkOption == 1){
            scripts = GetAtPath<MonoScript>(inputPath);
        }
        if(checkOption == 2){
            scripts = GetAllFiles<MonoScript>(inputPath);
        }

        foreach (MonoScript script in scripts){
            ScriptDetails tScriptDetails = new ScriptDetails();
            tScriptDetails.script = script;
            AllScripts.Add(tScriptDetails);
        }

        GameObject[] gos = GetAllFiles<GameObject>(inputPath);

        foreach(GameObject go in gos){
            foreach (Object obj in EditorUtility.CollectDependencies(new UnityEngine.Object[] {go})) {
                
                if(obj is AudioClip){
                    AudioClip clip = obj as AudioClip;
                    
                    int check = 1;
                    foreach(SoundDetails details in AllSounds){
                        if(details.clip == clip){
                            check = 0;
                            break;
                        }
                    }
                    if(check == 1){
                        SoundDetails tSoundDetails = new SoundDetails();
                        tSoundDetails.clip = clip;
                        AllSounds.Add(tSoundDetails);
                    }
                }
                if(obj is MonoScript){
                    MonoScript script = obj as MonoScript;
                    Debug.Log(script);
                    int check = 1;
                    foreach(ScriptDetails details in AllScripts){
                        if(details.script == script){
                            check = 0;
                            break;
                        }
                    }
                    if(check == 1){
                        ScriptDetails tScriptDetails = new ScriptDetails();
                        tScriptDetails.script = script;
                        AllScripts.Add(tScriptDetails);
                    }
                }
            }
           
        }
        foreach(TextureDetails tTextureDetails in AllTextures){
            tTextureDetails.memSizeBytes = TextureDetails.CalculateTextureSizeBytes(tTextureDetails.texture);
            TotalTextureMemory += tTextureDetails.memSizeBytes;
        }

        foreach(MeshDetails tMeshDetails in AllMeshes){
            TotalMeshVertices += tMeshDetails.mesh.vertexCount;
        }

    }
 
    //找到当前目录下全部文件
    private T[] GetAtPath<T>(string path){
        ArrayList al = new ArrayList();
        int dash = path.IndexOf("/");
        
        string shortPath;
        if(path.Equals("Assets")){
            shortPath = "";
        }
        else shortPath = path.Substring(dash);

        string[] fileEntries = Directory.GetFiles(Application.dataPath + shortPath , "*", SearchOption.AllDirectories);
      
        foreach (string fileName in fileEntries)
        {

            string temp = fileName.Replace("\\", "/");
            
            int index = temp.LastIndexOf("/");

            string localPath = path;
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


    //递归找到目录及子目录下全部文件
    private T[] GetAllFiles<T>(string path){
        ArrayList al = new ArrayList();
        int dash = path.IndexOf("/");
        string shortPath;
        if(path.Equals("Assets")){
            shortPath = "";
        }
        else shortPath = path.Substring(dash);

        GetFiles<T>(Application.dataPath + shortPath, al);

        T[] result = new T[al.Count];
 
        for (int i = 0; i < al.Count; i++){
            result[i] = (T) al[i];  
        }    
        return result;
    }



    private void GetFiles<T>(string path, ArrayList al){
        string[] fileEntries = Directory.GetFiles(path);
      
        foreach (string fileName in fileEntries){
            string temp = fileName.Replace("\\", "/");
            
            int index = temp.LastIndexOf("/");
            int PathIndex = path.IndexOf("Assets");
            string localPath = path.Substring(PathIndex);
            if (index > 0)
                localPath += temp.Substring(index);

            Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));
           
            if (t != null){  
                if(!al.Contains(t)){
                    al.Add(t);
                }
            }

        }
        string [] subdirectoryEntries = Directory.GetDirectories(path);
        foreach(string subdirectory in subdirectoryEntries){
            GetFiles<T>(subdirectory,al);
        }
    }
}
