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
    [HideInInspector] public SpawnableCratePlacer Placer;
    Renderer _placerRend;
    [HideInInspector] public string _lastBarcode;
    public static Func<CrateReference, GameObject> GetAsset;
    public static Action<SpawnableCratePlacer> EnsurePreview;
    [SerializeField] bool ExplorablePreview;
    private void SetChildHide(HideFlags hideFlags)
    { 
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.hideFlags = hideFlags;
        }
    }

    private void Update()
    {
        if (!Stalkers.Contains(this))
            Stalkers.Add(this);
        gameObject.hideFlags = HideFlags.DontSave;
        SetChildHide(ExplorablePreview ? HideFlags.DontSave : HideFlags.HideAndDontSave);
        if (!ExplorablePreview && (Selection.activeGameObject != gameObject))
            if (Selection.activeGameObject?.transform.IsChildOf(transform) ?? false)
                Selection.activeGameObject = transform.parent.gameObject;
        if (Placer == null)
        {
            DestroyImmediate(gameObject);
            return;
        }
        if (_placerRend == null)
            _placerRend = transform.parent.GetComponent<Renderer>();
        else _placerRend.enabled = transform.childCount == 0;
        if (_lastBarcode != Placer.spawnableCrateReference.Barcode.ID)
        { 
            _lastBarcode = Placer.spawnableCrateReference.Barcode.ID;
            for (var i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            GameObject newPrefab = GetAsset?.Invoke(Placer.spawnableCrateReference);
            if (newPrefab != null)
            {
                Transform newT = Instantiate(newPrefab, transform).transform;
                newT.localPosition = Vector3.zero;
                newT.localRotation = Quaternion.identity;

                foreach (SpawnableCratePlacer placer in newT.GetComponentsInChildren<SpawnableCratePlacer>())
                {
                    if (placer != Placer)
                        EnsurePreview.Invoke(placer);
                }
            }
        }
    }
}

#endif