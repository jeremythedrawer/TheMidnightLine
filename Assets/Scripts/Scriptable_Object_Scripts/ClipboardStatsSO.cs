using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ClipboardSO", menuName = "Midnight Line SOs / Clipboard SO")]
public class ClipboardStatsSO : ScriptableObject
{
    public enum ButtonTypeClicked
    {
        None,
        Tab,
        Page,
    }

    [Serializable] public struct ProfilePageData
    {
        public NPCTraits.Behaviours behaviours;
        public NPCTraits.Appearence appearence;
        public Color color;
        public bool spySelected;
    }
    public ProfilePageData[] profilePageArray;

    [Serializable] public struct TempStats
    {
        public float curDragMouseT;
        public float startDragMouseT;
        public float pageMaxScreenPosY;
        public float flipDist;
        public float rawHeight;
        public int curPageIndex;
        public ButtonTypeClicked buttonTypeClicked;
        public bool active;
        public bool canClickID;
        public bool flipUp;
        public bool hoverTab;
    }
    public TempStats tempStats;

    [Serializable] public struct CacheStats
    {
        public float imagesStartYPos;
        public float tabStartYPos;
        public float imagesTargetYPos;
        public float tabTargetYPos;
        public float ditherTransitionValue;
    }
    public CacheStats cacheStats;
}
