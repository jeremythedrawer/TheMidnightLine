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

    internal int activePageIndex;
    internal bool flippingPage;

    internal float startYPos;
    internal float hoverYPos;
    internal float targetYPos;
    internal bool active;
}
