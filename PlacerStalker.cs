using SLZ.Marrow.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
[ExecuteAlways]
public class PlacerStalker : MonoBehaviour
{
    public static List<PlacerStalker> Stalkers = new();
    public SpawnableCratePlacer Placer;
    Renderer _placerRend;
    public SpawnableCrate _lastCrate;
    public static Func<string, GameObject> GetAsset;
    private void Update()
    {
        if (!Stalkers.Contains(this))
            Stalkers.Add(this);
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        if (Selection.activeGameObject?.transform.IsChildOf(transform.parent) ?? false)
            Selection.activeGameObject = transform.parent.gameObject;
        if (Placer == null)
        {
            DestroyImmediate(gameObject);
            return;
        }
        if (_placerRend == null)
            _placerRend = transform.parent.GetComponent<Renderer>();
        else _placerRend.enabled = transform.childCount == 0;

        if (_lastCrate != Placer.spawnableCrateReference.EditorCrate)
        { 
            _lastCrate = Placer.spawnableCrateReference.EditorCrate;
            for (var i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            if (Placer.spawnableCrateReference.EditorCrate != null && Placer.spawnableCrateReference.EditorCrate.MainAsset != null)
            {
                GameObject newPrefab = GetAsset?.Invoke(Placer.spawnableCrateReference.EditorCrate.MainAsset.AssetGUID);
                if (newPrefab != null)
                {
                    Transform newT = Instantiate(newPrefab, transform).transform;
                    newT.localPosition = Vector3.zero;
                    newT.localRotation = Quaternion.identity;
                }
            }
        }
    }
}

#endif