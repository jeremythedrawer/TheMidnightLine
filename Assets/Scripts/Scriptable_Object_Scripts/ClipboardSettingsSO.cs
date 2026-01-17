using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ClipboardSettings_SO", menuName = "Midnight Line SOs / Clipbaord Settings SO")]
public class ClipboardSettingsSO : ScriptableObject
{
    public float imagesOnScreenYPos = 300;
    public float tabHoverYPos = 30;
    public float moveTime = 1;
    public float ditherTransitionTime = 1;
    [Range(0,1)]public float minDitherValue = 0.5f;
    [Range(0, 1)] public float dragToFlipPageThreshold = 0.125f;
    public float releasePageTime = 0.5f;
    public int randomPixelOffsetForPage = 10;
    public Page profilePagePrefab;
    public Page frontPagePrefab;
    public Image flippedPagePrefab;

}
