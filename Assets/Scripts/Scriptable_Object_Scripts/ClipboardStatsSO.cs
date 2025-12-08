using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ClipboardStatsSO", menuName = "Midnight Line SOs / ClipboardStats SO")]
public class ClipboardStatsSO : ScriptableObject
{
    [Serializable] public struct ProfilePageData
    {
        internal NPCTraits.Behaviours behaviours;
        internal NPCTraits.Appearence appearence;
    }
    internal List<ProfilePageData> profilePageData;

    internal float startYPos;
    internal bool active;
}
