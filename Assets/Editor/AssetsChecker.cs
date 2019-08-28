using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

// TODO: 目前FoundInGameObjects未使用

// XXXDetails: 用于储存XXX类型资源的信息

public class AssetsChecker : EditorWindow
{
    private class MaterialDetails
    {
        public string name;
        public string path;
        public Texture2D preview;
        public List<string> FoundInGameObjects;
    }

    private class TextureDetails
    {
        public string name;
        public string path;
        public Texture2D preview;

        public int memSizeBytes;

        public int height;
        public int width;

        public List<string> FoundInMaterials;
        public List<string> FoundInGameObjects;

        public static int CalculateTextureSizeBytes(Texture tTexture)
        {
            int tWidth = tTexture.width;
            int tHeight = tTexture.height;
            if (tTexture is Texture2D)
            {
                Texture2D tTex2D = tTexture as Texture2D;
                int bitsPerPixel = GetBitsPerPixel(tTex2D.format);
                int mipMapCount = tTex2D.mipmapCount;
                int mipLevel = 1;
                int tSize = 0;
                while (mipLevel <= mipMapCount)
                {
                    tSize += tWidth * tHeight * bitsPerPixel / 8;
                    tWidth = tWidth / 2;
                    tHeight = tHeight / 2;
                    mipLevel++;
                }
                return tSize;
            }

            if (tTexture is Cubemap)
            {
                Cubemap tCubemap = tTexture as Cubemap;
                int bitsPerPixel = GetBitsPerPixel(tCubemap.format);
                return tWidth * tHeight * 6 * bitsPerPixel / 8;
            }
            return 0;
        }

        public static int GetBitsPerPixel(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.Alpha8: //    Alpha-only texture format.
                    return 8;
                case TextureFormat.ARGB4444: //  A 16 bits/pixel texture format. Texture stores color with an alpha channel.
                    return 16;
                case TextureFormat.RGBA4444: //  A 16 bits/pixel texture format.
                    return 16;
                case TextureFormat.RGB24:   // A color texture format.
                    return 24;
                case TextureFormat.RGBA32:  //Color with an alpha channel texture format.
                    return 32;
                case TextureFormat.ARGB32:  //Color with an alpha channel texture format.
                    return 32;
                case TextureFormat.RGB565:  //   A 16 bit color texture format.
                    return 16;
                case TextureFormat.DXT1:    // Compressed color texture format.
                    return 4;
                case TextureFormat.DXT5:    // Compressed color with alpha channel texture format.
                    return 8;
                case TextureFormat.PVRTC_RGB2://     PowerVR (iOS) 2 bits/pixel compressed color texture format.
                    return 2;
                case TextureFormat.PVRTC_RGBA2://    PowerVR (iOS) 2 bits/pixel compressed with alpha channel texture format
                    return 2;
                case TextureFormat.PVRTC_RGB4://     PowerVR (iOS) 4 bits/pixel compressed color texture format.
                    return 4;
                case TextureFormat.PVRTC_RGBA4://    PowerVR (iOS) 4 bits/pixel compressed with alpha channel texture format
                    return 4;
                case TextureFormat.ETC_RGB4://   ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
                    return 4;

                case TextureFormat.BGRA32://     Format returned by iPhone camera
                    return 32;

                case TextureFormat.BC7:
                    return 8;

#if !UNITY_5 && !UNITY_5_3_OR_NEWER
            case TextureFormat.ATF_RGB_DXT1://   Flash-specific RGB DXT1 compressed color texture format.
            case TextureFormat.ATF_RGBA_JPG://   Flash-specific RGBA JPG-compressed color texture format.
            case TextureFormat.ATF_RGB_JPG://    Flash-specific RGB JPG-compressed color texture format.
            return 0; //Not supported yet  
#endif
            }
            return 0;
        }
    };

    private class MeshDetails
    {
        public string name;
        public string path;
        public Texture2D preview;

        public int vertexCount;
        public int triangles;

        public List<string> FoundInGameObjects;
    };

    private class ShaderDetails
    {
        public string name;
        public string path;

        public List<string> FoundInGameObjects;
        public List<string> FoundInMaterials;
    };

    public class SoundDetails
    {
        public string name;
        public string path;
        public Texture2D preview;

        public List<string> FoundInGameObjects;
    };

    public class ScriptDetails
    {
        public string name;
        public string path;

        public List<string> FoundInGameObjects;
    };

    static int MinWidth = 480;
    int TotalTextureMemory = 0;
    int TotalMeshVertices = 0;
    SearchOption searchOption = SearchOption.TopDirectoryOnly; // 仅检查该目录下的文件(不包括子目录)，或检查该目录下的所有文件(包括子目录)

    string inputPath = "Assets";
    ArrayList inputPathList = new ArrayList();
    int CurrentPath = 0;

    //Color defColor;

    Vector2 textureListScrollPos = new Vector2(0, 0);
    Vector2 materialListScrollPos = new Vector2(0, 0);
    Vector2 meshListScrollPos = new Vector2(0, 0);
    Vector2 shaderListScrollPos = new Vector2(0, 0);
    Vector2 soundListScrollPos = new Vector2(0, 0);
    Vector2 scriptListScrollPos = new Vector2(0, 0);

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

    enum SortType
    {
        AlphaSort, SizeSort, DependencySort
    };

    InspectType ActiveInspectType = InspectType.Materials;
    SortType ActiveSortType = SortType.AlphaSort;

    [MenuItem("Window/Assets Checker")]
    static void Init()
    {
        AssetsChecker window = (AssetsChecker)EditorWindow.GetWindow(typeof(AssetsChecker));
        window.minSize = new Vector2(MinWidth, 475);
    }

    // 显示并处理输入

    void OnGUI()
    {

        //defColor = GUI.color;


        //替换图标用的代码

        /* 
        byte[] fileData;

        Texture2D iconHandDraw1 = null;
        fileData = File.ReadAllBytes("Assets/icons/plus.png");
        iconHandDraw1 = new Texture2D(2,2);
        iconHandDraw1.LoadImage(fileData);
        
        Texture2D iconHandDraw2 = null;
        fileData = File.ReadAllBytes("Assets/icons/sort1.png");
        iconHandDraw2 = new Texture2D(2,2);
        iconHandDraw2.LoadImage(fileData);

        Texture2D iconHandDraw3 = null;
        fileData = File.ReadAllBytes("Assets/icons/sort2.png");
        iconHandDraw3 = new Texture2D(2,2);
        iconHandDraw3.LoadImage(fileData);

        Texture2D iconHandDraw4 = null;
        fileData = File.ReadAllBytes("Assets/icons/sort3.png");
        iconHandDraw4 = new Texture2D(2,2);
        iconHandDraw4.LoadImage(fileData);

        Texture2D iconHandDraw5 = null;
        fileData = File.ReadAllBytes("Assets/icons/refresh.png");
        iconHandDraw5 = new Texture2D(2,2);
        iconHandDraw5.LoadImage(fileData);
        */

        // 指定图标贴图

        Texture2D iconTexture = AssetPreview.GetMiniTypeThumbnail(typeof(Texture2D));
        Texture2D iconMaterial = AssetPreview.GetMiniTypeThumbnail(typeof(Material));
        Texture2D iconMesh = AssetPreview.GetMiniTypeThumbnail(typeof(Mesh));
        Texture2D iconShader = AssetPreview.GetMiniTypeThumbnail(typeof(Shader));
        Texture2D iconSound = AssetPreview.GetMiniTypeThumbnail(typeof(AudioClip));
        Texture2D iconScript = AssetPreview.GetMiniTypeThumbnail(typeof(MonoScript));
        Texture2D iconSortAlpha = EditorGUIUtility.FindTexture("AlphabeticalSorting");
        Texture2D iconSortDefault = EditorGUIUtility.FindTexture("DefaultSorting");
        Texture2D iconSortDepend = EditorGUIUtility.FindTexture("CustomSorting");
        Texture2D iconFolder = EditorGUIUtility.FindTexture("Folder Icon");
        Texture2D iconRefresh = EditorGUIUtility.FindTexture("vcs_Refresh");
        Texture2D iconPlus = EditorGUIUtility.FindTexture("Toolbar Plus");

        // 显示根据资源类型进行筛选的按钮

        GUILayout.Space(15);

        GUIContent[] guiObjs =
        {
            new GUIContent( iconTexture, "Textures" ),
            new GUIContent( iconMaterial, "Materials" ),
            new GUIContent( iconMesh, "Meshes" ),
            new GUIContent( iconShader, "Shaders" ),
            new GUIContent( iconSound, "Sounds" ),
            new GUIContent( iconScript, "Scripts" ),
        };

        GUILayoutOption[] options =
        {
            GUILayout.Width( 300 ),
            GUILayout.Height( 50 ),
        };

        //替换图标用的代码
        /* 
        GUIContent [] sortObjs = {
            new GUIContent(iconHandDraw4, "Sort by size"),
            new GUIContent(iconHandDraw3, "Sort Alphabetically"),
            new GUIContent(iconHandDraw2, "Sort by Dependency"),
        };
        */

        // 显示根据条件进行排序的按钮

        GUIContent[] sortObjs = {
            new GUIContent(iconSortAlpha, "Sort Alphabetically"),
            new GUIContent(iconSortDefault, "Sort by size"),
            new GUIContent(iconSortDepend, "Sort by Dependency"),
        };

        GUILayoutOption[] sortOptions =
        {
            GUILayout.Width(90),
            GUILayout.Height(30),
        };

        GUILayout.BeginHorizontal();
        GUILayout.Space(19);

        ActiveInspectType = (InspectType)GUILayout.Toolbar((int)ActiveInspectType, guiObjs, options);

        // 显示各种资源的数量

        GUILayout.Box((
            "Summary\n" +
            "Materials: " + AllMaterials.Count + "\n" +
            "Textures: " + AllTextures.Count + " - " + EditorUtility.FormatBytes(TotalTextureMemory)) + "\n" +
            "Meshes " + AllMeshes.Count + "\n" +
            "Shaders: " + AllShaders.Count + "\n" +
            "Sounds: " + AllSounds.Count + "\n" +
            "Scripts: " + AllScripts.Count + "\n",
            GUILayout.Width(150), GUILayout.Height(100));

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);

        GUILayout.Label(iconFolder, GUILayout.Width(30), GUILayout.Height(30));

        // 显示添加需检查的文件夹的输入框和确认添加的按钮，并处理输入

        inputPath = EditorGUILayout.TextField(inputPath, GUILayout.Width(350), GUILayout.Height(30));
        //if(GUILayout.Button(new GUIContent(iconHandDraw1, "Add to list"),  GUILayout.Width(30), GUILayout.Height(30))){
        if (GUILayout.Button(new GUIContent(iconPlus, "Add to list"), GUILayout.Width(30), GUILayout.Height(30)))
        {
            int check = 0;
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                // 和原列表中的元素进行一一比较，避免重复添加
                foreach (string s in inputPathList)
                {
                    if (s.Equals(inputPath))
                    {
                        check = 1;
                        break;
                    }
                }
                // 若待添加的文件夹不在原列表中，则确认添加
                if (check == 0)
                {
                    checkResources();
                    inputPathList.Add(inputPath);
                    CurrentPath++;
                }
            }
        }

        // 显示清空检查列表的按钮，并处理输入

        //if(GUILayout.Button(new GUIContent(iconHandDraw5, "Clear list"), GUILayout.Width(30), GUILayout.Height(30))){
        if (GUILayout.Button(new GUIContent(iconRefresh, "Clear list"), GUILayout.Width(30), GUILayout.Height(30)))
        {
            clearResources();
            searchOption = SearchOption.TopDirectoryOnly;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        ActiveSortType = (SortType)GUILayout.Toolbar((int)ActiveSortType, sortObjs, sortOptions);

        GUILayout.Space(258);

        // 显示手动刷新资源列表的按钮，并处理输入

        // 注：这是一个单向操作，重复点击也不能使searchOptions回到1
        // TODO: 需要在显示上做一些修改

        if (GUILayout.Button("Check all assets", GUILayout.Width(100), GUILayout.Height(30)))
        {
            if (searchOption != SearchOption.AllDirectories)
            {
                searchOption = SearchOption.AllDirectories;
                checkResources();
            }
        }
        GUILayout.EndHorizontal();


        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 根据选择的资源类型显示对应种类的资源列表

        switch (ActiveInspectType)
        {

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

        // 根据选择的条件对资源列表进行排序
        // TODO: 增加复合条件选项

        switch (ActiveSortType)
        {

            case SortType.AlphaSort:
                AllTextures.Sort(delegate (TextureDetails details1, TextureDetails details2) { return string.Compare(details1.name, details2.name); });
                AllMaterials.Sort(delegate (MaterialDetails details1, MaterialDetails details2) { return string.Compare(details1.name, details2.name); });
                AllMeshes.Sort(delegate (MeshDetails details1, MeshDetails details2) { return string.Compare(details1.name, details2.name); });
                AllShaders.Sort(delegate (ShaderDetails details1, ShaderDetails details2) { return string.Compare(details1.name, details2.name); });
                AllSounds.Sort(delegate (SoundDetails details1, SoundDetails details2) { return string.Compare(details1.name, details2.name); });
                AllScripts.Sort(delegate (ScriptDetails details1, ScriptDetails details2) { return string.Compare(details1.name, details2.name); });
                break;

            case SortType.SizeSort:
                AllTextures.Sort(delegate (TextureDetails details1, TextureDetails details2) { return details2.memSizeBytes - details1.memSizeBytes; });
                AllMeshes.Sort(delegate (MeshDetails details1, MeshDetails details2) { return details2.vertexCount - details1.vertexCount; });
                break;

            case SortType.DependencySort:
                AllTextures.Sort(delegate (TextureDetails details1, TextureDetails details2) { return details2.FoundInMaterials.Count - details1.FoundInMaterials.Count; });
                AllShaders.Sort(delegate (ShaderDetails details1, ShaderDetails details2) { return details2.FoundInMaterials.Count - details1.FoundInMaterials.Count; });
                break;

        }
    }

    // ListXXX：显示各种资源列表

    void ListMaterials()
    {

        materialListScrollPos = EditorGUILayout.BeginScrollView(materialListScrollPos);

        foreach (MaterialDetails mat in AllMaterials)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.Box(mat.preview, GUILayout.Width(50), GUILayout.Height(50));
            if (GUILayout.Button(new GUIContent(mat.name, mat.name), GUILayout.Width(150), GUILayout.Height(50)))
            {
                Selection.activeObject = (Material)AssetDatabase.LoadAssetAtPath(mat.path, typeof(Material));
            }
            //GUILayout.Label(AssetDatabase.GetAssetPath(mat.material), GUILayout.Width(150), GUILayout.Height(50));
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    void ListTextures()
    {

        textureListScrollPos = EditorGUILayout.BeginScrollView(textureListScrollPos);

        foreach (TextureDetails tex in AllTextures)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.Box(tex.preview, GUILayout.Width(50), GUILayout.Height(50));
            if (GUILayout.Button(new GUIContent(tex.name, tex.name), GUILayout.Width(150), GUILayout.Height(50)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(tex.path, typeof(Texture));
            }
            Texture2D iconMaterials = AssetPreview.GetMiniTypeThumbnail(typeof(Material));
            if (GUILayout.Button(new GUIContent(tex.FoundInMaterials.Count.ToString(), iconMaterials, "Materials"), GUILayout.Width(60), GUILayout.Height(50)))
            {
                List<Object> objects = new List<Object>();
                Object obj = null;
                foreach (string path in tex.FoundInMaterials)
                {
                    obj = AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                    if (obj != null)
                        objects.Add(obj);
                }
                Selection.objects = objects.ToArray();
            }
            GUILayout.Box(tex.width.ToString() + " x " + tex.height.ToString() + "\n" +
            EditorUtility.FormatBytes(tex.memSizeBytes) + "\n"

            , GUILayout.Width(120), GUILayout.Height(50));

            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    void ListMeshes()
    {
        meshListScrollPos = EditorGUILayout.BeginScrollView(meshListScrollPos);

        foreach (MeshDetails mes in AllMeshes)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.Box(mes.preview, GUILayout.Width(50), GUILayout.Height(50));
            if (GUILayout.Button(new GUIContent(mes.name, mes.name), GUILayout.Width(150), GUILayout.Height(50)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(mes.path, typeof(Mesh));
            }
            GUILayout.Box(mes.vertexCount.ToString() + " vertices\n" + mes.triangles + " Traingles\n", GUILayout.Width(100), GUILayout.Height(50));

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    void ListShaders()
    {

        shaderListScrollPos = EditorGUILayout.BeginScrollView(shaderListScrollPos);

        foreach (ShaderDetails sdr in AllShaders)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            Texture2D iconShader = AssetPreview.GetMiniTypeThumbnail(typeof(Shader));
            GUILayout.Box(iconShader, GUILayout.Width(50), GUILayout.Height(50));
            if (GUILayout.Button(new GUIContent(sdr.name, sdr.name), GUILayout.Width(150), GUILayout.Height(50)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(sdr.path, typeof(Shader));
            }
            Texture2D iconMaterials = AssetPreview.GetMiniTypeThumbnail(typeof(Material));
            if (GUILayout.Button(new GUIContent(sdr.FoundInMaterials.Count.ToString(), iconMaterials, "Materials"), GUILayout.Width(60), GUILayout.Height(50)))
            {
                List<Object> objects = new List<Object>();
                Object obj = null;
                foreach (string path in sdr.FoundInMaterials)
                {
                    obj = AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                    if (obj != null)
                        objects.Add(obj);
                }
                Selection.objects = objects.ToArray();
            }

            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    void ListSounds()
    {
        soundListScrollPos = EditorGUILayout.BeginScrollView(soundListScrollPos);

        foreach (SoundDetails snd in AllSounds)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.Box(snd.preview, GUILayout.Width(50), GUILayout.Height(50));
            if (GUILayout.Button(new GUIContent(snd.name, snd.name), GUILayout.Width(150), GUILayout.Height(50)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(snd.path, typeof(AudioClip));
            }
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    void ListScripts()
    {
        scriptListScrollPos = EditorGUILayout.BeginScrollView(scriptListScrollPos);

        foreach (ScriptDetails scp in AllScripts)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            Texture2D iconScript = AssetPreview.GetMiniTypeThumbnail(typeof(MonoScript));
            GUILayout.Box(iconScript, GUILayout.Width(50), GUILayout.Height(50));
            if (GUILayout.Button(new GUIContent(scp.name, scp.name), GUILayout.Width(150), GUILayout.Height(50)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(scp.path, typeof(MonoScript));
            }
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    // 清空所有资源列表、目录列表和统计数据

    void clearResources()
    {
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

    // 调用时机：目录列表改变，检索模式改变

    void checkResources()
    {
        string[] paths = null;
        Material[] materials = { };

        // TODO: inputPathList从未被使用，一直使用的是当前的inputPath
        materials = GetFiles<Material>(inputPath, searchOption, out paths);

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            MaterialDetails tMaterialDetails = new MaterialDetails();
            tMaterialDetails.FoundInGameObjects = new List<string>();
            tMaterialDetails.name = material.name;
            tMaterialDetails.path = paths[i];
            tMaterialDetails.preview = AssetPreview.GetMiniThumbnail(material);
            AllMaterials.Add(tMaterialDetails);
        }

        //TODO: 目前只会找到有material引用的Texture和Shader，这样做不符合我们找到冗余(未引用)资源的目的

        //找到material使用的texture和shader
        foreach (string path in paths)
        {
            foreach (string p in AssetDatabase.GetDependencies(path))
            {
                Texture tTexture = (Texture)AssetDatabase.LoadAssetAtPath(p, typeof(Texture));
                if (tTexture != null)
                {
                    int check = 0;
                    foreach (TextureDetails details in AllTextures)
                    {
                        if (details.path == p)
                        {
                            check = 1;
                            details.FoundInMaterials.Add(path);
                            break;
                        }
                    }
                    if (check == 0)
                    {
                        TextureDetails tTextureDetails = new TextureDetails();
                        tTextureDetails.FoundInMaterials = new List<string>();
                        tTextureDetails.name = tTexture.name;
                        tTextureDetails.path = p;
                        tTextureDetails.preview = AssetPreview.GetMiniThumbnail(tTexture);
                        tTextureDetails.memSizeBytes = TextureDetails.CalculateTextureSizeBytes(tTexture);
                        tTextureDetails.width = tTexture.width;
                        tTextureDetails.height = tTexture.height;
                        tTextureDetails.FoundInMaterials.Add(path);
                        AllTextures.Add(tTextureDetails);
                    }
                }
                else
                {
                    Shader tShader = (Shader)AssetDatabase.LoadAssetAtPath(p, typeof(Shader));
                    if (tShader != null)
                    {
                        int check = 0;
                        foreach (ShaderDetails details in AllShaders)
                        {
                            if (details.path == p)
                            {
                                check = 1;
                                details.FoundInMaterials.Add(path);
                                break;
                            }
                        }
                        if (check == 0)
                        {
                            ShaderDetails tShaderDetails = new ShaderDetails();
                            tShaderDetails.FoundInMaterials = new List<string>();
                            tShaderDetails.FoundInGameObjects = new List<string>();
                            tShaderDetails.name = tShader.name;
                            tShaderDetails.path = p;
                            tShaderDetails.FoundInMaterials.Add(path);
                            AllShaders.Add(tShaderDetails);
                        }
                    }
                }
            }
        }


        Mesh[] meshes = { };
        meshes = GetFiles<Mesh>(inputPath, searchOption, out paths);

        for (int i = 0; i < meshes.Length; i++)
        {
            Mesh mesh = meshes[i];
            MeshDetails tMeshDetails = new MeshDetails();
            tMeshDetails.FoundInGameObjects = new List<string>();
            tMeshDetails.name = mesh.name;
            tMeshDetails.path = paths[i];
            tMeshDetails.preview = AssetPreview.GetAssetPreview(mesh);
            tMeshDetails.vertexCount = mesh.vertexCount;
            tMeshDetails.triangles = mesh.triangles.Length;
            AllMeshes.Add(tMeshDetails);
        }

        AudioClip[] clips = { };
        clips = GetFiles<AudioClip>(inputPath, searchOption, out paths);

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            SoundDetails tSoundDetails = new SoundDetails();
            tSoundDetails.FoundInGameObjects = new List<string>();
            tSoundDetails.name = clip.name;
            tSoundDetails.path = paths[i];
            tSoundDetails.preview = AssetPreview.GetAssetPreview(clip);
            AllSounds.Add(tSoundDetails);
        }

        MonoScript[] scripts = { };
        scripts = GetFiles<MonoScript>(inputPath, searchOption, out paths);

        for (int i = 0; i < scripts.Length; i++)
        {
            MonoScript script = scripts[i];
            ScriptDetails tScriptDetails = new ScriptDetails();
            tScriptDetails.FoundInGameObjects = new List<string>();
            tScriptDetails.name = script.name;
            tScriptDetails.path = paths[i];
            AllScripts.Add(tScriptDetails);
        }

        GameObject[] gos = GetFiles<GameObject>(inputPath, searchOption, out paths);

        foreach (string path in paths)
        {
            foreach (string p in AssetDatabase.GetDependencies(path))
            {
                AudioClip clip = (AudioClip)AssetDatabase.LoadAssetAtPath(p, typeof(AudioClip));
                if (clip != null)
                {
                    int check = 1;
                    foreach (SoundDetails details in AllSounds)
                    {
                        if (details.path == p)
                        {
                            check = 0;
                            details.FoundInGameObjects.Add(path);
                            break;
                        }
                    }

                    if (check == 1)
                    {
                        SoundDetails tSoundDetails = new SoundDetails();
                        tSoundDetails.FoundInGameObjects = new List<string>();
                        tSoundDetails.name = clip.name;
                        tSoundDetails.path = p;
                        tSoundDetails.preview = AssetPreview.GetAssetPreview(clip);
                        tSoundDetails.FoundInGameObjects.Add(path);
                        AllSounds.Add(tSoundDetails);
                    }
                }
                else
                {
                    MonoScript script = (MonoScript)AssetDatabase.LoadAssetAtPath(p, typeof(MonoScript));
                    if (script != null)
                    {
                        int check = 1;
                        foreach (ScriptDetails details in AllScripts)
                        {
                            if (details.path == p)
                            {
                                check = 0;
                                details.FoundInGameObjects.Add(path);
                                break;
                            }
                        }
                        if (check == 1)
                        {
                            ScriptDetails tScriptDetails = new ScriptDetails();
                            tScriptDetails.FoundInGameObjects = new List<string>();
                            tScriptDetails.name = script.name;
                            tScriptDetails.path = p;
                            tScriptDetails.FoundInGameObjects.Add(path);
                            AllScripts.Add(tScriptDetails);
                        }
                    }
                }
            }
        }

        foreach (TextureDetails tTextureDetails in AllTextures)
        {
            TotalTextureMemory += tTextureDetails.memSizeBytes;
        }

        foreach (MeshDetails tMeshDetails in AllMeshes)
        {
            TotalMeshVertices += tMeshDetails.vertexCount;
        }

    }

    // 根据searchOption找到当前目录下全部文件，或当前目录下及子目录下的所有文件

    private T[] GetFiles<T>(string path, SearchOption option, out string[] paths)
        where T : UnityEngine.Object
    {
        List<T> al = new List<T>();
        List<string> pl = new List<string>();

        string shortPath = "";
        if (path.StartsWith("Assets", StringComparison.CurrentCultureIgnoreCase))
        {
            if (path.Length > "Assets".Length)
                shortPath = path.Substring("Assets".Length);

            string fullPath = Application.dataPath + shortPath;

            if (Directory.Exists(fullPath))
            {
                // (剪枝)根据T只检索对应后缀名的文件
                string searchPattern = "*";
                Type typeParameterType = typeof(T);
                if (typeParameterType == typeof(GameObject))
                {
                    searchPattern = "*.prefab";
                }
                else if (typeParameterType == typeof(MonoScript))
                {
                    searchPattern = "*.cs";
                }
                else if (typeParameterType == typeof(Material))
                {
                    searchPattern = "*.mat";
                }
                else if (typeParameterType == typeof(AudioClip))
                {
                    searchPattern = "*.mp3*.ogg*.wav*.aiff*.aif*.mod*.it*.s3m*.xm";
                }

                string[] fileEntries = Directory.GetFiles(fullPath, searchPattern, option);

                foreach (string filePath in fileEntries)
                {
                    string temp = filePath.Replace("\\", "/");

                    string localPath = "Assets" + temp.Substring(Application.dataPath.Length);

                    T t = (T)AssetDatabase.LoadAssetAtPath(localPath, typeof(T));

                    if (t != null)
                    {
                        al.Add(t);
                        pl.Add(localPath);
                    }
                }
            }

            // (异常处理)如果path不存在会报错
            else
            {
                Debug.LogWarning("AssetsChecker: " + fullPath + " not exist!");
            }
        }

        // (异常处理)如果path不以Assets开头会报错
        else
        {
            Debug.LogWarning("AssetsChecker: " + path + " not start with Assets!");
        }

        paths = pl.ToArray();
        return al.ToArray();
    }

}
