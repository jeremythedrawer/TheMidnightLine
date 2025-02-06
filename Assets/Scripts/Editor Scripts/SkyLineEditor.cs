using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkyLine))]
public class SkyLineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SkyLine skyLine = (SkyLine)target;

        if (GUILayout.Button("Apply Size"))
        {
            if (skyLine.spriteRenderer != null && skyLine.parallaxController != null)
            {
                Camera cam = Camera.main;
                skyLine.parallaxController.GetParralaxData(cam);
                skyLine.ApplyLength();
            }
            else
            {
                Debug.LogError("References are null");
            }
        }
    }
}
