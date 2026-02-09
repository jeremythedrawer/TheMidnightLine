using System.Collections.Generic;
using UnityEngine;
using static Atlas;

[CreateAssetMenu(fileName = "AtlasMotion", menuName = " Atlas / Atlas Motion")]
public class AtlasMotionSO : AtlasBaseSO
{
    public EntityType entityType;
    public AtlasClip[] clips;
    public int framesPerSecond = 30;

    public Dictionary<int, AtlasClip> clipDict;

    private void OnEnable()
    {
        UpdateClipDictionary();
    }

    public void UpdateClipDictionary()
    {
        clipDict = BuildClipKeys(clips);
    }
}

