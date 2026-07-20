using UnityEngine;
using System.Collections.Generic;

using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using static Atlas;

[ExecuteAlways]
public class SimpleMotionPlayer : MonoBehaviour
{
    public AtlasRenderer atlasRenderer;

    [Header("Gernerated")]
    public AtlasSO atlas;
    public AtlasClip clip;
    public int clipIndex;

    private void Update()
    {
        atlasRenderer.PlayClip(ref clip);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SimpleMotionPlayer))]
public class SimpleMotionPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SimpleMotionPlayer motionPlayer = (SimpleMotionPlayer)target;
        if (motionPlayer.atlasRenderer == null)
        {
            DrawDefaultInspector();
            return;
        }

        motionPlayer.atlas = motionPlayer.atlasRenderer.atlas;
        
        if (MotionEnumDictionary.TryGetValue(motionPlayer.atlas.entityMotionType, out Type motionEnumType))
        {
            Enum curEnum = (Enum)Enum.ToObject(motionEnumType, motionPlayer.clipIndex);
            Enum newEnum = EditorGUILayout.EnumPopup("Motion", curEnum);

            int clipIndex = Convert.ToInt32(newEnum);
            
            if (motionPlayer.atlas.clipDict.TryGetValue(clipIndex, out AtlasClip clip))
            {
                motionPlayer.clip = motionPlayer.atlas.clipDict[clipIndex];
                motionPlayer.clipIndex = clipIndex;
                EditorUtility.SetDirty(motionPlayer);
            }
        }
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
    }
}


#endif