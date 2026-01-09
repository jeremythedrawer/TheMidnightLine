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
    public ProfilePageData[] profilePageArray;

    [Serializable] public struct TempStats
    {
        public float flipPageAtlasUnitSize;
        public float imagesStartYPos;
        public float tabStartYPos;
        public float imagesTargetYPos;
        public float tabTargetYPos;
        public float ditherTransitionValue;
        public float curDragMouseT;
        public float prevCurDragMouseT;
        public float pageMaxScreenPosY;
        public int curPageIndex;
        public bool active;
        public bool canClickID;
        public bool flipUp;
    }
    public TempStats tempStats;
}
