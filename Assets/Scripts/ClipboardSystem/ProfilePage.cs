using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePage : MonoBehaviour
{
    [SerializeField] TMP_Text behavioursText;
    [SerializeField] TMP_Text appearenceText;
    [SerializeField] Image borderImage;
    [SerializeField] Image pageImage;
    public RectTransform rectTransform;
    public Image flipPageImage;

    [SerializeField] ClipboardStatsSO clipboardStats;

    [Serializable] public struct Stats
    {
        internal Color borderColor;
        internal int pageIndex;
    }
    [SerializeField] Stats stats;

    private void Awake()
    {
        flipPageImage.material = Instantiate(flipPageImage.material);
        flipPageImage.enabled = false;
        behavioursText.fontMaterial = Instantiate(behavioursText.fontMaterial);
        appearenceText.fontMaterial = Instantiate(appearenceText.fontMaterial);
        behavioursText.fontMaterial.SetColor("_FaceColor", Color.black);
        appearenceText.fontMaterial.SetColor("_FaceColor", Color.black);

    }
    public void SetPageParams(int pageIndex)
    {
        stats.pageIndex = pageIndex;
        NPCTraits.Behaviours rawBehave = clipboardStats.profilePageArray[pageIndex].behaviours;
        NPCTraits.Appearence rawAppear = clipboardStats.profilePageArray[pageIndex].appearence;

        int behaveCount = Bitwise.GetSetBitCount((long)rawBehave);
        int[] behavePositions = new int[behaveCount];
        int appearPosition = 0;

        int posIndex = 0;
        int flagIndex = 0;
        foreach (NPCTraits.Behaviours flag in Enum.GetValues(typeof(NPCTraits.Behaviours)))
        {
            if (flag == NPCTraits.Behaviours.Nothing) continue;

            if ((rawBehave & flag) != 0)
            {
                behavePositions[posIndex] = flagIndex;
                posIndex++;
            }
            flagIndex++;
        }
        flagIndex = 0;
        foreach (NPCTraits.Appearence flag in Enum.GetValues(typeof(NPCTraits.Appearence)))
        {
            if (flag == NPCTraits.Appearence.Nothing) continue;

            if ((rawAppear & flag) != 0)
            {
                appearPosition = flagIndex;
                break;
            }
            flagIndex++;
        }

        string behaveText = "";
        for (int i = 0; i < behavePositions.Length; i++)
        {
            behaveText += "- " + NPCTraits.behaviourDescriptions[behavePositions[i]];
            if (i < behavePositions.Length - 1) behaveText += "\n";
        }

        behavioursText.text = behaveText;
        appearenceText.text = "- " + NPCTraits.appearenceDescriptions[appearPosition];

        borderImage.color = clipboardStats.profilePageArray[pageIndex].color;

        rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, -(clipboardStats.profilePageArray.Length - 1 - pageIndex));
    }
    public void Flipped(bool flippedDown)
    {
        flipPageImage.enabled = !flippedDown;
        borderImage.enabled = flippedDown;
        behavioursText.enabled = flippedDown;
        appearenceText.enabled = flippedDown;
        pageImage.enabled = flippedDown;
    }
}
