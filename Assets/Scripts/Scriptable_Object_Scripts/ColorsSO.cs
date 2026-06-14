using System;
using System.Collections.Generic;
using UnityEngine;
using static NPC;

[CreateAssetMenu(fileName = "Colors", menuName = "Midnight Line SOs / Colors")]
public class ColorsSO : ScriptableObject
{
    [Serializable]public struct BehaviourColors
    {
        public Behaviours behaviours;
        public Color color;
    }


    public Color32 blackColor;
    public Color32 whiteColor;

    public Color32 ticketCheckColor;

    [Range(0,1)]public float dayNight;
    [Range(0,1)]public float dayNightFactor;

    [Header("Generated")]
    public BehaviourColors[] behaviourColors;
}
