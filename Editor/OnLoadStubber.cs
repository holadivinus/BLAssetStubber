using Codice.Client.Common.GameUI;
using System;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

[InitializeOnLoad] // Ensure the constructor is called when the scripts reload in the Editor.
public class OnLoadStubber
{
    public static string SLZAAPath => s_slzAAPath ??= Path.Combine(EditorPrefs.GetString("bonelabs_folder", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BONELAB\\"), "BONELAB_Steam_Windows64_Data\\StreamingAssets\\aa");
    private static string s_slzAAPath;
    public static string LocalLowPath => s_localLowPath ??= Path.GetFullPath(Path.Combine(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName, "LocalLow\\"));
    private static string s_localLowPath;
    public static string ModsPath => s_ModsPath ??= Path.Combine(LocalLowPath, "Stress Level Zero\\BONELAB\\Mods\\");
    private static string s_ModsPath;
    public static string WrongModsString => s_wrongModsString ??= 
        $"{Application.companyName}\\{Directory.GetParent(Application.dataPath).Name}\\Mods";
    private static string s_wrongModsString;
    static OnLoadStubber()
    {


        EditorApplication.update += StubSwapper.UpdateTick;
        Addressables.ResourceManager.InternalIdTransformFunc += SLZAssetURLFixer;
        CompilationPipeline.compilationStarted += (obj) =>
        {
            StubSwapper.OnWillSaveAssets(null);
            Compiling = true; 
        };
        CompilationPipeline.compilationFinished += (o) => Compiling = false;
    } 
    public static bool Compiling;
    public static string SLZAssetURLFixer(IResourceLocation assetURL)
        => SLZAssetURLFixerString(assetURL.InternalId);
    
    public static string SLZAssetURLFixerString(string assetURL)
    {
        if (assetURL.StartsWith("Library/com.unity.addressables/aa/Windows"))
            return Path.GetFullPath(SLZAAPath + assetURL.Substring(41));
        if (Path.GetFullPath(assetURL).StartsWith(LocalLowPath))
        { // example assetURL:    C:\Users\Holadivinus\AppData\LocalLow\DefaultCompany\BLTextureStubSystem\Mods\Rexmeck.WeaponPack\selectivededupe_assets_packages\com.unity.render-pipelines.universal\shaders\unlit.shader.bundle
          // example LocalLowPath C:\Users\Holadivinus\AppData\LocalLow\
          //                      C:\Users\Holadivinus\AppData\LocalLow\Stress Level Zero\BONELAB\Mods\Rexmeck.WeaponPack
            assetURL = Path.GetFullPath(assetURL);
            Debug.Log("FIN: " + assetURL.Replace(WrongModsString, "Stress Level Zero\\BONELAB\\Mods\\"));
            return assetURL.Replace(WrongModsString, "Stress Level Zero\\BONELAB\\Mods\\");
        
        }
        return assetURL;
    }
}    
