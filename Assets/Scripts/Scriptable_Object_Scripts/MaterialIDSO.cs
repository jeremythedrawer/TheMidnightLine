using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialIDSO", menuName = "Midnight Line SOs / Material ID SO")]
public class MaterialIDSO : ScriptableObject
{
    [Serializable] public struct IDs
    {
        internal int normAnimTime;
        internal int fontColor;
        internal int ditherValue;
        internal int color;
        internal int zPos;
        internal int alpha;
        internal int mainTex;
        internal int hovered;
        internal int selected;
        internal int targetPosition;
        internal int density;
        internal int scrollTime;
        internal int brightness;
        internal int atlasSize;
    }
    internal IDs ids;

#if UNITY_EDITOR
    private void SetMaterialPropertyIDs()
    {
        ids.normAnimTime = Shader.PropertyToID("_NormAnimTime");
        ids.fontColor = Shader.PropertyToID("_FaceColor");
        ids.ditherValue = Shader.PropertyToID("_DitherValue");
        ids.color = Shader.PropertyToID("_Color");
        ids.zPos = Shader.PropertyToID("_ZPos");
        ids.alpha = Shader.PropertyToID("_Alpha");
        ids.mainTex = Shader.PropertyToID("_MainTex");
        ids.hovered = Shader.PropertyToID("_Hovered");
        ids.selected = Shader.PropertyToID("_Selected");
        ids.targetPosition = Shader.PropertyToID("_TargetPosition");
        ids.density = Shader.PropertyToID("_Density");
        ids.scrollTime = Shader.PropertyToID("_ScrollTime");
        ids.brightness = Shader.PropertyToID("_Brightness");
        ids.atlasSize = Shader.PropertyToID("_AtlasSize");

        EditorUtility.SetDirty(this);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SetAllMaterialPropertyIDs()
    {
        MaterialIDSO[] all = Resources.FindObjectsOfTypeAll<MaterialIDSO>();
        foreach (MaterialIDSO so in all)
        {
            so.SetMaterialPropertyIDs();
        }
    }
#endif
}
