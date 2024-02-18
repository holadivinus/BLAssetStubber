using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Newtonsoft.Json;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
using System.Drawing.Printing;
using NUnit.Framework;
using UnityEngine.TextCore.Text;
using System.Linq;
using System;
using Unity.VisualScripting;
using static UnityEditor.Progress;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using static UnityEngine.Rendering.DebugUI.MessageBox;
using System.Reflection;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Text.RegularExpressions;

public class AssetStubGUI : EditorWindow
{
    [MenuItem("Tools/Stub Creation Wizard")]
    public static void ShowWindow() => GetWindow<AssetStubGUI>("Stub Creation Wizard");
    
    private static bool Started;
    public Dictionary<Type, string> templates = new Dictionary<Type, string>() {
        { typeof(Material), "Assets/BonelabAssetStubber/EmptyMaterialAsset.mat" },
        { typeof(Shader), "Assets/BonelabAssetStubber/EmptyShaderAsset.shader" },
        { typeof(Mesh), "Assets/BonelabAssetStubber/EmptyMeshAsset.asset" },
        { typeof(GameObject), "Assets/BonelabAssetStubber/EmptyPrefabAsset.prefab" },
        { typeof(Texture2D), "Assets/BonelabAssetStubber/EmptyTextureAsset.png" }
    };
    public Dictionary<WizardMode, Type> modetypes = new Dictionary<WizardMode, Type>() {
        { WizardMode.Material, typeof(Material) },
        { WizardMode.Shader, typeof(Shader) },
        { WizardMode.Mesh, typeof(Mesh) },
        { WizardMode.Prefab, typeof(GameObject) },
        { WizardMode.Texture, typeof(Texture2D) },
        { WizardMode.All, typeof(UnityEngine.Object) },
    };

    private static bool pathConfirmed;
    private static bool failure;
    private void OnGUI()
    {
        if (!Started)
        {
            Started = true;
            SetWizardMode(WizardMode.All);
        }
        if (!pathConfirmed)
            pathConfirmed = Directory.Exists(OnLoadStubber.SLZAAPath);
        if (!pathConfirmed)
        {
            GUILayout.Label("Confirm/Input your Bonelabs game folder:");
            EditorPrefs.SetString("bonelabs_folder", GUILayout.TextField(EditorPrefs.GetString("bonelabs_folder", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BONELAB\\")));
            if (GUILayout.Button("Confirm"))
            {
                if (!Directory.Exists(OnLoadStubber.SLZAAPath))
                {
                    failure = true;
                }
                else pathConfirmed = true;
            }
            if (failure)
                GUILayout.Label("nuh uh, you put a bad path in");

            if (!pathConfirmed)
                return;
        }

        // Create a GUIStyle that supports rich text
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.richText = true;
        textStyle.alignment = TextAnchor.MiddleCenter;

        // Set the font size and make it bold
        textStyle.fontSize = 20;
        textStyle.fontStyle = FontStyle.Bold;

        // Use a GUILayout.Label with HTML-like tags for underlined text
        GUILayout.Label("Asset Stub Wizard", textStyle);

        float rectWidth = 370;
        EditorGUI.DrawRect(new Rect((position.width-rectWidth)/2, 25, rectWidth, 2), Color.white);

        GUIStyle selectedButtonStyle = new GUIStyle(GUI.skin.button);
        selectedButtonStyle.normal.textColor = Color.black;
        selectedButtonStyle.hover.textColor = Color.black;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        for (int i = 0; i < (int)WizardMode.Max; i++)
        {
            WizardMode mode = (WizardMode)i;
            if (GUILayout.Button($"{mode}s", CurrentMode == mode ? selectedButtonStyle : GUI.skin.button))
                SetWizardMode(mode);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUI.DrawRect(new Rect() { y = 90, size = new Vector2(position.width, position.height) }, new Color(.2f, .2f, .2f));

        GUILayout.BeginVertical();
        GUILayout.Space(50);
        GUILayout.EndVertical();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Search: ", new GUIStyle(GUI.skin.label) { alignment=TextAnchor.UpperRight });
        _searchQuery = GUILayout.TextField(_searchQuery, new GUIStyle(GUI.skin.textField) { fixedWidth = position.width/2 } );
        if (GUILayout.Button(AssetDatabase.LoadAssetAtPath<Texture>("Assets/BonelabAssetStubber/MagnifyingGlass.png"),
            new GUIStyle(GUI.skin.button) { fixedWidth = 25 }))
            SearchAssets();
        if (GUILayout.Button("(Debug) Reload Stubs"))
        {
            StubSwapper.StartReload();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        rectWidth = position.width * .95f;
        EditorGUI.DrawRect(new Rect() { x = (position.width - rectWidth)/2, y = 135, size = new Vector2(rectWidth, position.height) }, new Color(.15f, .15f, .15f));

        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.Space(position.width * .05f);
        height = 0;

        int rows = 6;
        int cols = 6;
        GUILayout.BeginVertical();
        for (int y = 0; y < rows; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < cols; x++)
            {
                int idx = (x + (y * rows));
                if (idx >= PreviewedAssets.Length || PreviewedAssets[idx] == null)
                {
                    GUILayout.Space(position.width / (cols + 1));
                    continue;
                }
                GUILayout.BeginVertical();
                GUIStyle style = new GUIStyle(GUI.skin.button) { fixedHeight = position.width / (cols+1), };
                if (GUILayout.Button(":3", style))
                    CreateStub(PreviewedAssets[idx]);
                Rect preRect = GUILayoutUtility.GetLastRect();
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.width *= .8f;
                rect.height *= .8f;
                rect.x += preRect.width * .1f;
                rect.y += preRect.height * .1f;
                Texture preview = GetPreview(PreviewedAssets[idx]);
                if (preview != null)
                    EditorGUI.DrawTextureTransparent(rect, preview);
                GUILayout.Label(PreviewedAssets[idx].SimpleAssetName(), new GUIStyle(GUI.skin.label) { fixedWidth = position.width / (cols + 1), });
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        GUILayout.Space(position.width * .05f);
        GUILayout.EndHorizontal();
        
    }
    private string _searchQuery = "";
    const int searchCount = 30;
    public string[] PreviewedAssets = new string[searchCount];
    public Dictionary<string, Texture> Previews = new Dictionary<string, Texture>();
    public enum WizardMode { All, Prefab, Material, Texture, Mesh, Shader, AudioClip, Max }
    public WizardMode CurrentMode;
    private void SetWizardMode(WizardMode mode)
    {
        CurrentMode = mode;
        PreviewedAssets = new string[searchCount];
    }
    private void SearchAssets()
    {
        PreviewedAssets = new string[searchCount];

        int used = 0;

        IEnumerable<KeyValuePair<object, IList<IResourceLocation>>> assets = StubSwapper.SLZAssetLocator.Locations.Where(l => modetypes[CurrentMode].IsAssignableFrom(l.Value.First().ResourceType)).Where(s => !s.Key.ToString().EndsWith(".bundle"));
        if (!string.IsNullOrWhiteSpace(_searchQuery))
            assets = assets.SortBySimilarity(s => s.Key.ToString().SimpleAssetName(), _searchQuery);

        foreach (var item in assets)
        {
            PreviewedAssets[used++] = item.Key.ToString();
            if (used == searchCount)
                return;
        }
    }
    private void CreateStub(string assetPath)
    {
        // Get current folder:
        UnityEngine.Object asset = StubSwapper.GetExternalAsset<UnityEngine.Object>(assetPath);

        System.Type projectWindowUtilType = typeof(ProjectWindowUtil);
        MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
        object obj = getActiveFolderPath.Invoke(null, new object[0]);
        string currentFolder = obj.ToString();

        string templatePath = templates[asset.GetType()];
        string endPath = currentFolder + "/STUB_" + StubSwapper.ExternalAssetsReverse[asset].Replace("/", "%-%") + "." + templatePath.Split('.').Last();
        AssetDatabase.CopyAsset(templatePath, endPath);
        UnityEngine.Object stubAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(endPath);
        using SerializedObject serializedAsset = new SerializedObject(stubAsset);
        stubAsset.name = Path.GetFileNameWithoutExtension(endPath);

        switch (asset.GetType().Name)
        {
            case "Material":
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.GetComponent<Renderer>().sharedMaterial = stubAsset as Material;
                PositionInfrontCameraAndSave(sphere);
                break;
            case "GameObject":
                PositionInfrontCameraAndSave((GameObject)PrefabUtility.InstantiatePrefab((GameObject)stubAsset));
                break;
        }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(endPath);
    }
    [MenuItem("CONTEXT/Texture/Select STUB Texture")]
    [MenuItem("CONTEXT/Material/Select STUB Material")]
    public static void SelectStubMaterial(MenuCommand command)
    {
        if (StubSwapper.ExternalAssetsReverse.TryGetValue(command.context, out string path))
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("STUB_" + path).FirstOrDefault()));
            Debug.Log(path);
        }
    }

    private void PositionInfrontCameraAndSave(GameObject gobj)
    {
        Selection.activeGameObject = gobj;

        // Access the main SceneView
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.camera != null)
        {
            Camera sceneCamera = sceneView.camera;

            // Calculate the position in front of the camera
            Vector3 newPosition = sceneCamera.transform.position + sceneCamera.transform.forward * 5f; // 5 units in front

            // Position the selected GameObject 
            Selection.activeGameObject.transform.position = newPosition;
            Selection.activeGameObject.transform.rotation = Quaternion.LookRotation(-sceneCamera.transform.forward, sceneCamera.transform.up) * Quaternion.Euler(0, 90, 0);

            // Mark the object as dirty to ensure changes are saved
            EditorUtility.SetDirty(Selection.activeGameObject);
            EditorSceneManager.SaveOpenScenes();

            // Optionally, focus the SceneView on the newly positioned object
            SceneView.lastActiveSceneView.FrameSelected();
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }

    private int height;
    private Texture GetPreview(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;
        if (Previews.TryGetValue(assetPath, out Texture preview))
            if (preview != null)
                return preview;

        UnityEngine.Object asset = null;

        WizardMode handling = CurrentMode;
        if (CurrentMode == WizardMode.All) 
        {
            asset = StubSwapper.GetExternalAsset<UnityEngine.Object>(assetPath);
            if (asset is GameObject)
                handling = WizardMode.Prefab;
            else if (asset is Texture)
                handling = WizardMode.Texture;
            else if (asset is Mesh) handling = WizardMode.Mesh;
            else if (asset is Shader) handling = WizardMode.Shader;
            else if (asset is Material) handling = WizardMode.Material;
            else if (asset is AudioClip) handling = WizardMode.AudioClip;
        }
        
        switch (handling)
        {
            case WizardMode.Prefab:
                GameObject prefabAsset = StubSwapper.GetExternalAsset<GameObject>(assetPath);

                GameObject previewObj = Instantiate(prefabAsset);

                previewObj.transform.position += Vector3.up * height++ * 2 + (Vector3.left * height * 10);

                // start recording
                Camera objCam = new GameObject("PreviewCam").AddComponent<Camera>();
                objCam.targetTexture = new RenderTexture(100, 100, 16);
                objCam.transform.position = (Vector3.right * 1.15f) + previewObj.transform.position;
                objCam.transform.LookAt(previewObj.transform);

                // Figure preview's bounds
                bool hasBounds = false;
                Bounds previewBounds = new Bounds();
                foreach (Renderer renderer in previewObj.GetComponentsInChildren<Renderer>())
                    if (!hasBounds)
                    {
                        hasBounds = true;
                        previewBounds = renderer.bounds;
                    } else previewBounds.Encapsulate(renderer.bounds);

                float CalculateDistance2(Camera camera, Bounds bounds)
                {
                    float objectSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                    float cameraView = Mathf.Min(camera.fieldOfView, camera.fieldOfView * camera.aspect);
                    float distance = 1.1f * objectSize / Mathf.Sin(cameraView * Mathf.Deg2Rad / 2f);

                    return distance;
                }
                CalculateDistance2(objCam, previewBounds);

                for (int i = 0; i < 5; i++)
                    StubSwapper.NextFrame.Enqueue(null);
                StubSwapper.NextFrame.Enqueue(() =>
                {
                    //cam.Render();
                    objCam.targetTexture = null;

                    DestroyImmediate(objCam.gameObject);
                    DestroyImmediate(previewObj.gameObject);
                });

                Previews[assetPath] = objCam.targetTexture;
                return objCam.targetTexture;
            case WizardMode.Material:
                Material previewMat = StubSwapper.GetExternalAsset<Material>(assetPath);

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.GetComponent<Renderer>().sharedMaterial = previewMat;
                sphere.transform.position += Vector3.up * height++ * 2;

                // Take a snapshot
                Camera cam = new GameObject("PreviewCam").AddComponent<Camera>();
                cam.targetTexture = new RenderTexture(100, 100, 16);
                cam.transform.position = (Vector3.back * 1.15f) + sphere.transform.position;

                for (int i = 0; i < 5; i++)
                    StubSwapper.NextFrame.Enqueue(null);
                StubSwapper.NextFrame.Enqueue(() =>
                {
                    //cam.Render();
                    cam.targetTexture = null;

                    DestroyImmediate(cam.gameObject);
                    DestroyImmediate(sphere.gameObject);
                });
                
                Previews[assetPath] = cam.targetTexture;
                return cam.targetTexture;
            case WizardMode.Texture:
                Texture2D remote = StubSwapper.GetExternalAsset<Texture2D>(assetPath);
                Previews[assetPath] = remote;
                return remote;
            case WizardMode.Mesh:
                Mesh previewMesh = StubSwapper.GetExternalAsset<Mesh>(assetPath);

                GameObject sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere2.GetComponent<MeshFilter>().sharedMesh = previewMesh;
                sphere2.transform.position += Vector3.up * height++ * 2 + (Vector3.right * height * 10);

                // Take a snapshot
                Camera cam2 = new GameObject("PreviewCam").AddComponent<Camera>();
                cam2.targetTexture = new RenderTexture(100, 100, 16);
                cam2.transform.position = (Vector3.back * 1.15f) + sphere2.transform.position;

                // Calculate bounding box of the mesh
                Bounds bounds = sphere2.GetComponent<Renderer>().bounds;
                float CalculateDistance(Camera camera, Bounds bounds)
                {
                    float objectSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                    float cameraView = Mathf.Min(camera.fieldOfView, camera.fieldOfView * camera.aspect);
                    float distance = 1.1f * objectSize / Mathf.Sin(cameraView * Mathf.Deg2Rad / 2f);

                    return distance;
                }
                // Calculate camera distance
                float cameraDistance = CalculateDistance(cam2, bounds);

                // Adjust camera position and rotation
                Vector3 objectCenter = bounds.center;
                cam2.transform.position = objectCenter - cam2.transform.forward * cameraDistance;
                cam2.transform.LookAt(objectCenter);
                

                for (int i = 0; i < 5; i++)
                    StubSwapper.NextFrame.Enqueue(null);
                StubSwapper.NextFrame.Enqueue(() =>
                {
                    //cam.Render();
                    cam2.targetTexture = null;

                    DestroyImmediate(cam2.gameObject);
                    DestroyImmediate(sphere2.gameObject);
                });

                Previews[assetPath] = cam2.targetTexture;
                return cam2.targetTexture;
            case WizardMode.Shader:
                return AssetDatabase.LoadAssetAtPath<Texture>("Assets/BonelabAssetStubber/ShaderPreview.png");
            case WizardMode.AudioClip:
                //asset = deet.Load<AudioClip>();
                break;
        }
        Texture2D newPreview = new Texture2D(100, 100);

        return newPreview;
    }
}


static class Sorting
{
    public static string SimpleAssetName(this string name) => Regex.Replace(name.Split('/').Last().ToLower(), @"\d+|texture|material|shader|prefab|_", "");
    public static IEnumerable<T> SortBySimilarity<T>(this IEnumerable<T> source, Func<T, string> conv, string target)
    {
        return source.Select(item => new
        {
            Item = item,
            Distance = LevenshteinDistance(conv.Invoke(item), target),
            StartsWith = conv.Invoke(item).StartsWith(target, StringComparison.OrdinalIgnoreCase),
            Contains = conv.Invoke(item).Contains(target, StringComparison.OrdinalIgnoreCase)
        })
        .OrderBy(x => !x.StartsWith) // Prefer items that start with the target
        .ThenBy(x => !x.Contains)    // Then prefer items that contain the target
        .ThenBy(x => x.Distance).Select(a => a.Item);     // Finally, sort by Levenshtein distance
    }

    public static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
        {
            return string.IsNullOrEmpty(b) ? 0 : b.Length;
        }

        if (string.IsNullOrEmpty(b))
        {
            return a.Length;
        }

        int lengthA = a.Length;
        int lengthB = b.Length;
        var distances = new int[lengthA + 1, lengthB + 1];

        for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
        for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

        for (int i = 1; i <= lengthA; i++)
            for (int j = 1; j <= lengthB; j++)
            {
                int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }

        return distances[lengthA, lengthB];
    }
}