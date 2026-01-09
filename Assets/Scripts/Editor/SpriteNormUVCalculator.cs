using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteNormUVCalculator", menuName = "Editor SOs / SpriteNorm UV Calculator")]
public class SpriteNormUVCalculator : ScriptableObject
{
    [SerializeField] Sprite sprite;
    [SerializeField] Material material;
    [Serializable] struct MaterialIDs
    {
        internal int uvPosition;
        internal int uvSize;
    }
    [SerializeField] MaterialIDs materialIDs;
    public void SetNormUV()
    {
        materialIDs.uvPosition = Shader.PropertyToID("_UVPosition");
        materialIDs.uvSize = Shader.PropertyToID("_UVSize");

        Vector2 uvPosition = sprite.textureRect.position;
        Vector2 uvSize = sprite.textureRect.size;
        material.SetVector(materialIDs.uvPosition, uvPosition);
        material.SetVector(materialIDs.uvSize, uvSize);
    }
}

[CustomEditor(typeof(SpriteNormUVCalculator))]
public class SpriteNormUVCalculatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SpriteNormUVCalculator uvCalc = (SpriteNormUVCalculator)target;

        if (GUILayout.Button("Set Normal UV"))
        {
            uvCalc.SetNormUV();
        }
    }
}
