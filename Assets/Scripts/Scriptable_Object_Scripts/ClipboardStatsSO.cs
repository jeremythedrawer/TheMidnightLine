using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ClipboardSO", menuName = "Midnight Line SOs / Clipboard SO")]
public class ClipboardStatsSO : ScriptableObject
{
    [Serializable] public struct ProfilePageData
    {
        internal NPCTraits.Behaviours behaviours;
        internal NPCTraits.Appearence appearence;
        internal Color color;
        internal bool spySelected;
    }
    internal ProfilePageData[] profilePageArray;

    [Serializable] public struct MaterialIDs
    {
        public int normAnimTime;
    }
    internal MaterialIDs materialIDs;

    [Serializable] public struct TempStats
    {
        internal float imagesStartYPos;
        internal float tabStartYPos;
        internal float imagesTargetYPos;
        internal float tabTargetYPos;
        internal int curPageIndex;
        internal bool active;
        internal bool flippingPage;
    }
    internal TempStats tempStats;
}
