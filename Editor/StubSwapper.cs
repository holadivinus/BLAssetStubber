using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using SLZ.Marrow.Warehouse;
using UnityEngine.ResourceManagement.ResourceLocations;

public class StubSwapper : AssetModificationProcessor
{
    public static Dictionary<string, UnityEngine.Object> ExternalAssets = new Dictionary<string, UnityEngine.Object>();
    public static Dictionary<UnityEngine.Object, string> ExternalAssetsReverse = new Dictionary<UnityEngine.Object, string>();
    public static ResourceLocationCollection SLZAssetLocator => s_slzAssetLocator ??= new ResourceLocationCollection();
    private static ResourceLocationCollection s_slzAssetLocator;

    public class ResourceLocationCollection
    {
        public ResourceLocationCollection()
        {
            EditorUtility.DisplayProgressBar("Gathering Assets...", "Loading SLZ Catalog", 0);
            AssetLocators = new List<ResourceLocationMap>()
            { (ResourceLocationMap)Addressables.LoadContentCatalogAsync("Library/com.unity.addressables/aa/Windows\\catalog.json").WaitForCompletion()};

            string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appdataPath = Directory.GetParent(appdataPath).FullName;
            appdataPath = Path.Combine(appdataPath, "LocalLow\\Stress Level Zero\\BONELAB\\Mods\\");

            var modCatalogs = Directory.EnumerateDirectories(appdataPath).Select(dir => Directory.EnumerateFiles(dir).FirstOrDefault(file => Path.GetFileName(file).StartsWith("catalog_") && Path.GetFileName(file).EndsWith(".json")));

            int i = 0;
            float iMax = modCatalogs.Count();
            foreach (string catalogPath in modCatalogs)
            {
                EditorUtility.DisplayProgressBar("Gathering Assets...", $"Loading {Directory.GetParent(catalogPath).Name} Catalog", i++/iMax);
                AssetLocators = AssetLocators.Append((ResourceLocationMap)Addressables.LoadContentCatalogAsync(catalogPath).WaitForCompletion());
            }

            EditorUtility.DisplayProgressBar("Combining Asset Lists", "", 1);
            foreach (ResourceLocationMap curLocatorMap in AssetLocators)
                foreach (KeyValuePair<object, IList<IResourceLocation>> mapping in curLocatorMap.Locations)
                    if (!Locations.ContainsKey(mapping.Key))
                        Locations[mapping.Key] = mapping.Value;   
            
            EditorUtility.ClearProgressBar();
        }
        public IEnumerable<ResourceLocationMap> AssetLocators;
        public Dictionary<object,  IList<IResourceLocation>> Locations = new();
    }

    public static T GetExternalAsset<T>(string key) where T : UnityEngine.Object
    {
        if (key.Contains("deduped_assets_cubemap") || string.IsNullOrEmpty(key) || key.EndsWith(".bundle")) 
        {
            Debug.Log("|GEA| nulled invalid key: " + key);
            return null; //temp idiocy since im tired
        }

        if (ExternalAssets.TryGetValue(key, out UnityEngine.Object value))
        {
            Debug.Log("|GEA| Found Cached Asset: " + value);
            return value as T;
        }
        else if (SLZAssetLocator.Locations.TryGetValue(key, out IList<IResourceLocation> locations) && locations.Count > 0)
        {
            IResourceLocation location = locations[0];
            Debug.Log("|GEA| P1. Loading new Asset: " + key);

            UnityEngine.Object loaded = null;

            if (typeof(T).IsAssignableFrom(location.ResourceType))
            {
                loaded = Addressables.LoadAssetAsync<T>(key).WaitForCompletion();
                ExternalAssets[key] = loaded;
                ExternalAssetsReverse[loaded] = key;
                if (loaded is Texture2D tex)
                    tex.minimumMipmapLevel = 0;
                Debug.Log("|GEA| P2. Asset Loaded: " + loaded);
                return (T)loaded;
            }
            else
            {
                Debug.Log("|GEA| PF. Type Mismatch for address: " + location.ResourceType + ", expected: " + typeof(T).Name);
                Debug.Log("|GEA| PF. Returning null.");
                return null;
            }
        }
        Debug.Log("|GEA| Key not found anywhere! " + key);
        return null;
    }


    private static List<int> listOfInts = new List<int>();
    private static List<Material> listOfMats = new List<Material>();
    
    public static string[] OnWillSaveAssets(string[] paths)
    {
        if (OnLoadStubber.Compiling)
            return paths; 
        Debug.Log("OnWillSaveAssets"); 
        //PlacerStalker.Kill.Invoke();
        // Stubbing Prefabs
        foreach (GameObject gameObj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!gameObj.name.StartsWith("STUB_GAMEOBJECT_PREFAB_"))
                continue;

            string stubPath = AssetDatabase.FindAssets("STUB_" + gameObj.name.Substring(23).Replace("/", "%-%")).FirstOrDefault();
            if (string.IsNullOrEmpty(stubPath))
            {
                UnityEngine.Object.DestroyImmediate(gameObj);
                continue;
            }

            Transform parent = gameObj.transform.parent;
            Vector3 pos = gameObj.transform.position;
            Quaternion rot = gameObj.transform.rotation;
            Vector3 siz = gameObj.transform.localScale;

            UnityEngine.Object.DestroyImmediate(gameObj);
            GameObject instanced = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(stubPath)), parent);

            instanced.transform.position = pos;
            instanced.transform.rotation = rot;
            instanced.transform.localScale = siz;
        }

        // Material & Tex Stubbing
        Texture curTex = null;
        foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
        {
            if (string.IsNullOrEmpty(renderer.gameObject.scene.name) || renderer.GetComponentInParent<PlacerStalker>())
                continue;
            Debug.Log("Processing Renderer: " + renderer.gameObject.name);

            int matCounter = 0;
            renderer.GetSharedMaterials(listOfMats);
            foreach (Material curMat in listOfMats)
            {
                if (curMat == null)
                {
                    Debug.Log(" - Mat was Null");
                    matCounter++;
                    continue;
                }
                Debug.Log(" - Processing Mat: " + curMat.name);

                if (!ExternalAssetsReverse.TryGetValue(curMat, out string matAddr)) // If mat is not stub, check if texs are stub
                {
                    Debug.Log(" - - Processing Mat's Textures");
                    curMat.GetTexturePropertyNameIDs(listOfInts);
                    foreach (int id in listOfInts)
                    {
                        if ((curTex = curMat.GetTexture(id)) == null)
                            continue;

                        if (!ExternalAssetsReverse.TryGetValue(curTex, out string texAddr))
                            continue;

                        string stubPath2 = AssetDatabase.FindAssets("STUB_" + texAddr.Replace("/", "%-%")).FirstOrDefault();
                        if (stubPath2 != null)
                            curMat.SetTexture(id, AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(stubPath2)));
                        else
                            curMat.SetTexture(id, null); 
                    }
                    Debug.Log(" - - - i dont feel like detailing tex stubs rn");
                    matCounter++;
                    continue; 
                }

                string stubPath = AssetDatabase.FindAssets("STUB_" + matAddr.Replace("/", "%-%")).FirstOrDefault();
                Debug.Log(" - - Finding Stub for Mat addr: " + matAddr);
                if (stubPath != null) 
                {
                    Material[] sharedMats = renderer.sharedMaterials;
                    sharedMats[matCounter++] = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(stubPath));
                    renderer.sharedMaterials = sharedMats;
                    Debug.Log(" - - Saving, set asset to mat: " + sharedMats[matCounter - 1]);
                } 
                else
                {
                    Material[] sharedMats = renderer.sharedMaterials;
                    sharedMats[matCounter++] = null;
                    renderer.sharedMaterials = sharedMats;
                    Debug.Log(" - - Saving, set asset to null");
                }
            }
            Debug.Log(" - renderer processing fin");
        }


        // Stubbing Meshes
        foreach (MeshFilter meshFilter in Resources.FindObjectsOfTypeAll<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null || !ExternalAssetsReverse.TryGetValue(meshFilter.sharedMesh, out string meshAddr))
                continue;

            string stubPath = AssetDatabase.FindAssets("STUB_" + meshAddr.Replace("/", "%-%")).FirstOrDefault();
            if (stubPath != null)
                meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(stubPath));
            else meshFilter.sharedMesh = null;
        }

        _justSaved = true;
        //Debug.Log("Saving (Swapping Refs to Stubs)");

        return paths;
    }
    private static bool _justSaved;
    public static Queue<Action> NextFrame = new Queue<Action>();
    private static int waiter;
    public static void StartReload()
    {
        OnWillSaveAssets(null); 
        s_reloadComplete = false;
    }
    public static bool s_reloadComplete;
    public static void UpdateTick()
    {
        if (OnLoadStubber.Compiling)
            return;
        PlacerStalker.GetAsset ??= (key) => GetExternalAsset<GameObject>(key);
        if (!s_reloadComplete)
        {
            Debug.Log("Reload Complete!");
            s_reloadComplete = true;
            AssetBundle.UnloadAllAssetBundles(true);
            Addressables.ClearResourceLocators();
            
            s_slzAssetLocator = null;
            StubSwapper.ExternalAssets.Clear(); 
            StubSwapper.ExternalAssetsReverse.Clear();  
             
            OnPostSave();
            _justSaved = false;
        } else
        if (_justSaved)
        {
            _justSaved = false;
            OnPostSave();
        }
        if (NextFrame.Count > 0 && waiter-- < 0)
        {
            NextFrame.Dequeue()?.Invoke(); 
            waiter = 5;
        }
    }
    public static void OnPostSave()
    {
        if (OnLoadStubber.Compiling) 
            return;
        Debug.Log("OnPostSave");

        AssetWarehouse.OnReady(() =>  
        {
            NextFrame.Enqueue(() =>
            {
                foreach (SpawnableCratePlacer placer in Resources.FindObjectsOfTypeAll<SpawnableCratePlacer>())
                {
                    if (placer == null || string.IsNullOrEmpty(placer.gameObject.scene.name) || placer.GetComponentInChildren<PlacerStalker>() != null)
                        continue;

                    PlacerStalker newStalk = new GameObject("Preview").AddComponent<PlacerStalker>();
                    newStalk.transform.SetParent(placer.transform, false);
                    newStalk.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    newStalk.Placer = placer;
                }
                foreach (var item in PlacerStalker.Stalkers)
                {
                    item._lastCrate = null;
                }
            });
        });
        
        // Un-Stubbing Materials & Textures
        foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
        {
            if (string.IsNullOrEmpty(renderer.gameObject.scene.name))
                continue;
            int matCounter = -1;
            renderer.GetSharedMaterials(listOfMats);
            foreach (Material curMat in listOfMats)
            {
                matCounter++;
                if (curMat == null)
                    continue;

                string curMatPath = curMat.name;
                if (!curMatPath.StartsWith("STUB_")) // If mat is not a stub, check if textures are
                {
                    curMat.GetTexturePropertyNameIDs(listOfInts);
                    foreach (int id in listOfInts)
                    {
                        // Un-Stubbing Textures
                        Texture curTex;
                        if ((curTex = curMat.GetTexture(id)) == null)
                            continue;

                        string curTexPath = curTex.name;
                        if (!curTexPath.StartsWith("STUB_"))
                            continue;

                        Texture2D loadedTex = GetExternalAsset<Texture2D>(curTexPath.Substring(5).Replace("%-%", "/"));
                        if (loadedTex != null)
                            curMat.SetTexture(id, loadedTex);
                    }
                    continue;
                }

                Material loadedMat = GetExternalAsset<Material>(curMatPath.Substring(5).Replace("%-%", "/"));

                if (loadedMat != null)
                {
                    Material[] sharedMats = renderer.sharedMaterials;
                    sharedMats[matCounter] = loadedMat;
                    renderer.sharedMaterials = sharedMats;
                }
            }
        }
         
        // Un-Stubbing Meshes
        foreach (MeshFilter meshFilter in Resources.FindObjectsOfTypeAll<MeshFilter>())
        {
            if (string.IsNullOrEmpty(meshFilter.gameObject.scene.name))
                continue;
            if (meshFilter.sharedMesh == null)
                continue;

            string curMeshPath = meshFilter.sharedMesh.name;
            if (!curMeshPath.StartsWith("STUB_"))
                continue;

            Mesh loadedMesh = GetExternalAsset<Mesh>(curMeshPath.Substring(5).Replace("%-%", "/"));

            if (loadedMesh != null)
                meshFilter.sharedMesh = loadedMesh;
        }

        // Un-Stubbing Prefabs 
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject == null || !PrefabUtility.IsPartOfPrefabInstance(gameObject))
                continue;
            //Debug.Log(gameObject);
            string prefabPath = PrefabUtility.GetCorrespondingObjectFromSource(gameObject).name;


            if (!prefabPath.StartsWith("STUB_"))
                continue;

            GameObject loadedPrefab = GetExternalAsset<GameObject>(prefabPath.Substring(5).Replace("%-%", "/"));
            //Debug.Log(loadedPrefab);
            if (loadedPrefab != null)
            {
                Transform parent = gameObject.transform.parent;
                Vector3 pos = gameObject.transform.position;
                Quaternion rot = gameObject.transform.rotation;
                Vector3 siz = gameObject.transform.localScale;

                UnityEngine.Object.DestroyImmediate(PrefabUtility.GetNearestPrefabInstanceRoot(gameObject));

                GameObject instanced = UnityEngine.Object.Instantiate(loadedPrefab, parent);
                instanced.transform.position = pos;
                instanced.transform.rotation = rot;
                instanced.transform.localScale = siz;
                instanced.name = "STUB_GAMEOBJECT_PREFAB_" + prefabPath.Substring(5);
            }
        }
    }
}