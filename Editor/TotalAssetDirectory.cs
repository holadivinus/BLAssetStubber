using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

public partial class StubSwapper
{
    public class TotalAssetDirectory
    {
        public TotalAssetDirectory()
        {
            try
            {
                ////EditorUtility.DisplayProgressBar("Gathering Assets...", "Loading SLZ Catalog", 0);
                AssetLocators = new List<ResourceLocationMap>() {
                (ResourceLocationMap)Addressables.LoadContentCatalogAsync("Library/com.unity.addressables/aa/Windows\\catalog.json").WaitForCompletion()};
                ResourceLocationMap bad = AssetLocators.First();

                var modCatalogs = Directory.EnumerateDirectories(OnLoadStubber.ModsPath).Select(dir => Directory.EnumerateFiles(dir).FirstOrDefault(file => Path.GetFileName(file).StartsWith("catalog_") && Path.GetFileName(file).EndsWith(".json")));

                int i = 0;
                float iMax = modCatalogs.Count();
                foreach (string catalogPath in modCatalogs)
                {
                    ////EditorUtility.DisplayProgressBar("Gathering Assets...", $"Loading {Directory.GetParent(catalogPath).Name} Catalog", i++/iMax);
                    AssetLocators = AssetLocators.Append((ResourceLocationMap)Addressables.LoadContentCatalogAsync(catalogPath).WaitForCompletion());
                }

                ////EditorUtility.DisplayProgressBar("Combining Asset Lists", "", 1);
                foreach (ResourceLocationMap curLocatorMap in AssetLocators)
                    foreach (KeyValuePair<object, IList<IResourceLocation>> mapping in curLocatorMap.Locations)
                        if (!Locations.ContainsKey(mapping.Key))
                        {
                            if (curLocatorMap != bad)
                                Debug.Log(mapping.Key + " : " + mapping.Value.First());
                            Locations[mapping.Key] = mapping.Value; 
                        }
                 
                //EditorUtility.ClearProgressBar(); 
            } catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
        public IEnumerable<ResourceLocationMap> AssetLocators;
        public Dictionary<object,  IList<IResourceLocation>> Locations = new();

        public void PrintLastLocations()
        {
        }
    }
}