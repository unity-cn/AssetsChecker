using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

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
    const string defaultPath = "Resources/unity_builtin_extra";
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
    // TODO: 用generic重构

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
                Selection.activeObject = LoadAssetFromUniqueAssetPath<Material>(mat.path);
            }

            Texture2D iconGameObjects = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
            if (GUILayout.Button(new GUIContent(mat.FoundInGameObjects.Count.ToString(), iconGameObjects, "GameObjects"), GUILayout.Width(60), GUILayout.Height(50)))
            {
                List<Object> objects = new List<Object>();
                Object obj = null;
                foreach (string path in mat.FoundInGameObjects)
                {
                    obj = LoadAssetFromUniqueAssetPath<GameObject>(path);
                    if (obj != null)
                        objects.Add(obj);
                }
                Selection.objects = objects.ToArray();
            }

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
                Selection.activeObject = LoadAssetFromUniqueAssetPath<Texture>(tex.path);
            }
            Texture2D iconMaterials = AssetPreview.GetMiniTypeThumbnail(typeof(Material));
            if (GUILayout.Button(new GUIContent(tex.FoundInMaterials.Count.ToString(), iconMaterials, "Materials"), GUILayout.Width(60), GUILayout.Height(50)))
            {
                List<Object> objects = new List<Object>();
                Object obj = null;
                foreach (string path in tex.FoundInMaterials)
                {
                    obj = LoadAssetFromUniqueAssetPath<Material>(path);
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
                Selection.activeObject = LoadAssetFromUniqueAssetPath<Mesh>(mes.path);
            }

            Texture2D iconGameObjects = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
            if (GUILayout.Button(new GUIContent(mes.FoundInGameObjects.Count.ToString(), iconGameObjects, "GameObjects"), GUILayout.Width(60), GUILayout.Height(50)))
            {
                List<Object> objects = new List<Object>();
                Object obj = null;
                foreach (string path in mes.FoundInGameObjects)
                {
                    obj = LoadAssetFromUniqueAssetPath<GameObject>(path);
                    if (obj != null)
                        objects.Add(obj);
                }
                Selection.objects = objects.ToArray();
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
                Selection.activeObject = LoadAssetFromUniqueAssetPath<Shader>(sdr.path);
            }
            Texture2D iconMaterials = AssetPreview.GetMiniTypeThumbnail(typeof(Material));
            if (GUILayout.Button(new GUIContent(sdr.FoundInMaterials.Count.ToString(), iconMaterials, "Materials"), GUILayout.Width(60), GUILayout.Height(50)))
            {
                List<Object> objects = new List<Object>();
                Object obj = null;
                foreach (string path in sdr.FoundInMaterials)
                {
                    obj = LoadAssetFromUniqueAssetPath<Material>(path);
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
                Selection.activeObject = LoadAssetFromUniqueAssetPath<AudioClip>(snd.path);
            }

            Texture2D iconGameObjects = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
            if (GUILayout.Button(new GUIContent(snd.FoundInGameObjects.Count.ToString(), iconGameObjects, "GameObjects"), GUILayout.Width(60), GUILayout.Height(50)))
            {
                List<Object> objects = new List<Object>();
                Object obj = null;
                foreach (string path in snd.FoundInGameObjects)
                {
                    obj = LoadAssetFromUniqueAssetPath<GameObject>(path);
                    if (obj != null)
                        objects.Add(obj);
                }
                Selection.objects = objects.ToArray();
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
                Selection.activeObject = LoadAssetFromUniqueAssetPath<MonoScript>(scp.path);
            }

            Texture2D iconGameObjects = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
            if (GUILayout.Button(new GUIContent(scp.FoundInGameObjects.Count.ToString(), iconGameObjects, "GameObjects"), GUILayout.Width(60), GUILayout.Height(50)))
            {
                List<Object> objects = new List<Object>();
                Object obj = null;
                foreach (string path in scp.FoundInGameObjects)
                {
                    obj = LoadAssetFromUniqueAssetPath<GameObject>(path);
                    if (obj != null)
                        objects.Add(obj);
                }
                Selection.objects = objects.ToArray();
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

        // TODO: inputPathList从未被使用，一直使用的是当前的inputPath
        paths = GetFilePaths<Material>(inputPath, searchOption);

        //目前只会找到有Material引用的自定义Texture和Shader
        //对于被引用的默认Texture和Shader也进行显示

        //找到material使用的texture和shader
        foreach (string path in paths)
        {
            Material material = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
            if (material != null)
            {
                MaterialDetails tMaterialDetails = new MaterialDetails();
                tMaterialDetails.FoundInGameObjects = new List<string>();
                tMaterialDetails.name = material.name;
                tMaterialDetails.path = path;

                // 对于material的缩略图进行深拷贝
                Texture2D preview = AssetPreview.GetAssetPreview(material);
                tMaterialDetails.preview = new Texture2D(preview.width, preview.height);
                tMaterialDetails.preview.SetPixels32(preview.GetPixels32());
                tMaterialDetails.preview.Apply();

                AllMaterials.Add(tMaterialDetails);

                foreach (Object obj in EditorUtility.CollectDependencies(new UnityEngine.Object[] { material }))
                {
                    string p = AssetDatabase.GetAssetPath(obj);
                    if (p == defaultPath)
                        p = defaultPath + "::" + obj.name;

                    if (obj is Texture)
                    {
                        Texture tTexture = (Texture)obj;

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

                    else if (obj is Shader)
                    {
                        Shader tShader = (Shader)obj;

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

        paths = GetFilePaths<Mesh>(inputPath, searchOption);

        foreach (string path in paths)
        {
            Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath(path, typeof(Mesh));
            if (mesh != null)
            {
                MeshDetails tMeshDetails = new MeshDetails();
                tMeshDetails.FoundInGameObjects = new List<string>();
                tMeshDetails.name = mesh.name;
                tMeshDetails.path = path;
                tMeshDetails.preview = AssetPreview.GetAssetPreview(mesh);
                tMeshDetails.vertexCount = mesh.vertexCount;
                tMeshDetails.triangles = mesh.triangles.Length;
                AllMeshes.Add(tMeshDetails);
            }
        }

        paths = GetFilePaths<AudioClip>(inputPath, searchOption);

        foreach (string path in paths)
        {
            AudioClip clip = (AudioClip)AssetDatabase.LoadAssetAtPath(path, typeof(AudioClip));
            if (clip != null)
            {
                SoundDetails tSoundDetails = new SoundDetails();
                tSoundDetails.FoundInGameObjects = new List<string>();
                tSoundDetails.name = clip.name;
                tSoundDetails.path = path;
                tSoundDetails.preview = AssetPreview.GetAssetPreview(clip);
                AllSounds.Add(tSoundDetails);
            }
        }

        paths = GetFilePaths<MonoScript>(inputPath, searchOption);

        foreach (string path in paths)
        {
            MonoScript script = (MonoScript)AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));
            if (script != null)
            {
                ScriptDetails tScriptDetails = new ScriptDetails();
                tScriptDetails.FoundInGameObjects = new List<string>();
                tScriptDetails.name = script.name;
                tScriptDetails.path = path;
                AllScripts.Add(tScriptDetails);
            }
        }

        // TODO: 优化查重算法
        // TODO: 目前只能找到存为prefab的GameObject
        // 找到GameObject引用的内建资源也能找到

        paths = GetFilePaths<GameObject>(inputPath, searchOption);

        foreach (string path in paths)
        {
            GameObject gameObject = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
            if (gameObject != null)
            {
                foreach (Object obj in EditorUtility.CollectDependencies(new UnityEngine.Object[] { gameObject }))
                {
                    string p = AssetDatabase.GetAssetPath(obj);
                    if (p == defaultPath)
                        p = defaultPath + "::" + obj.name;

                    if (obj is Material)
                    {
                        Material material = (Material)obj;
                        int check = 1;
                        foreach (MaterialDetails details in AllMaterials)
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
                            MaterialDetails tMaterialDetails = new MaterialDetails();
                            tMaterialDetails.FoundInGameObjects = new List<string>();
                            tMaterialDetails.name = material.name;
                            tMaterialDetails.path = path;

                            // 对于material的缩略图进行深拷贝
                            Texture2D preview = AssetPreview.GetAssetPreview(material);
                            tMaterialDetails.preview = new Texture2D(preview.width, preview.height);
                            tMaterialDetails.preview.SetPixels32(preview.GetPixels32());
                            tMaterialDetails.preview.Apply();

                            tMaterialDetails.FoundInGameObjects.Add(path);
                            AllMaterials.Add(tMaterialDetails);
                        }
                    }

                    else if (obj is Mesh)
                    {
                        Mesh mesh = (Mesh)obj;
                        int check = 1;
                        foreach (MeshDetails details in AllMeshes)
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
                            MeshDetails tMeshDetails = new MeshDetails();
                            tMeshDetails.FoundInGameObjects = new List<string>();
                            tMeshDetails.name = mesh.name;
                            tMeshDetails.path = path;
                            tMeshDetails.preview = AssetPreview.GetAssetPreview(mesh);
                            tMeshDetails.vertexCount = mesh.vertexCount;
                            tMeshDetails.triangles = mesh.triangles.Length;
                            tMeshDetails.FoundInGameObjects.Add(path);
                            AllMeshes.Add(tMeshDetails);
                        }
                    }

                    else if (obj is AudioClip)
                    {
                        AudioClip clip = (AudioClip)obj;
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

                    else if (obj is MonoScript)
                    {
                        MonoScript script = (MonoScript)obj;
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

    private string[] GetFilePaths<T>(string path, SearchOption option)
        where T : UnityEngine.Object
    {
        List<string> paths = new List<string>();

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

                    paths.Add(localPath);
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

        return paths.ToArray();
    }

    public static T LoadAssetFromUniqueAssetPath<T>(string aAssetPath) where T : UnityEngine.Object
    {
        if (aAssetPath.Contains("::"))
        {
            string[] parts = aAssetPath.Split(new string[] { "::" }, System.StringSplitOptions.RemoveEmptyEntries);
            aAssetPath = parts[0];
            if (parts.Length > 1)
            {
                string assetName = parts[1];
                System.Type t = typeof(T);
                var assets = AssetDatabase.LoadAllAssetsAtPath(aAssetPath)
                    .Where(i => t.IsAssignableFrom(i.GetType())).Cast<T>();
                var obj = assets.Where(i => i.name == assetName).FirstOrDefault();
                if (obj == null)
                {
                    int id;
                    if (int.TryParse(parts[1], out id))
                        obj = assets.Where(i => i.GetInstanceID() == id).FirstOrDefault();
                }
                if (obj != null)
                    return obj;
            }
        }
        return AssetDatabase.LoadAssetAtPath<T>(aAssetPath);
    }
}
