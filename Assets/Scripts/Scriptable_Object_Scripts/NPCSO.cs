using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;


#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSO : ScriptableObject
{
    [Header("Components")]
    public Material material;
    public MaterialIDSO materialIDs;
    public AtlasSO atlas;
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundAccelation = 12f;

    [Header("Station")]
    public float maxDistanceDetection = 6.0f;

    public NPCTraits.Appearence appearence;
    public NPCTraits.Behaviours behaviours;

    [Header("Difficulty")]
    public Vector2 pickBehaviourDurationRange = new Vector2(10, 30);

    GraphicsBuffer atlasBuffer;

    [Serializable] public struct AnimEventPosData
    {
        public Vector2 position;
        public float time;
    }
    public AnimEventPosData[] smokeAnimPosData;

    private void OnEnable()
    {
        SetAtlasSpriteBuffer();
    }

    private void OnDisable()
    {
        
    }

    public void SetAtlasSpriteBuffer()
    {
        if (!material || !atlas)
        {
            Debug.LogWarning($"Material or Atlas not set on {name}", this);
            return;
        }
        atlasBuffer = Atlas.GetAtlasSpriteBuffer(atlas);

        material.SetBuffer(materialIDs.ids.atlasSprites, atlasBuffer);
    }
}
