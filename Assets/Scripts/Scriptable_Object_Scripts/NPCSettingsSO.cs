using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NPC_Settings_SO", menuName = "Midnight Line SOs / NPC Settings SO")]
public class NPCSettingsSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float groundAccelation = 12f;
    public float groundDecelaration = 14f;

    [Header("Station")]
    public float maxDistanceDetection = 6.0f;

    [Header("Colors")]
    public Color suspectColor = Color.red;

    public NPCTraits.Appearence appearence;
}
