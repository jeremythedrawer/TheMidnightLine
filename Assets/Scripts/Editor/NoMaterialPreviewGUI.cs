using UnityEditor;
using UnityEngine;

public class NoMaterialPreviewGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
    }

    public override void OnMaterialPreviewGUI(MaterialEditor materialEditor, Rect r,GUIStyle background)
    {
    }

    public override void OnMaterialPreviewSettingsGUI(MaterialEditor materialEditor)
    {
    }
}
