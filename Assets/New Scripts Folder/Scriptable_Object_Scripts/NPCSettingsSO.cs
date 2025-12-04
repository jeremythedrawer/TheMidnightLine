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

    [Flags]
    public enum Appearence
    {
        Nothing = 0,
        Heavy = 1 << 0,
        Tall = 1 << 1,
        Old = 1 << 2,
        Glasses = 1 << 3,
        SuitAndTie = 1 << 4,
        Bald = 1 << 5,
        DarkHair = 1 << 6,
        Dress = 1 << 7,
        Shorts = 1 << 8,
        Bag = 1 << 9,
        Headware = 1 << 10,
    }
    public Appearence appearence;
}
