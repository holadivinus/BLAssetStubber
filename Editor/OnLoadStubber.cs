using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

[InitializeOnLoad] // Ensure the constructor is called when the scripts reload in the Editor.
public class OnLoadStubber
{
    public static string SLZAAPath => Path.Combine(EditorPrefs.GetString("bonelabs_folder", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BONELAB\\"), "BONELAB_Steam_Windows64_Data\\StreamingAssets\\aa");
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
        return assetURL;
    }
}    
