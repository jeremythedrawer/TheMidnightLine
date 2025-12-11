using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePage : MonoBehaviour
{
    [Serializable] public struct ComponentData
    {
        public TMP_Text behavioursText;
        public TMP_Text appearenceText;
        public Image borderImage;
        public Image pageImage;
        public RectTransform rectTransform;
    }
    public ComponentData components;

    [Serializable] public struct SOData
    {
        public ClipboardStatsSO clipboardStats;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct Stats
    {
        internal Color borderColor;
        internal int pageIndex;
    }
    [SerializeField] Stats stats;

    private void OnEnable()
    {
        components.pageImage.material = Instantiate(components.pageImage.material);
    }
    public void SetPageParams(int pageIndex)
    {
        stats.pageIndex = pageIndex;
        NPCTraits.Behaviours rawBehave = soData.clipboardStats.profilePageArray[pageIndex].behaviours;
        NPCTraits.Appearence rawAppear = soData.clipboardStats.profilePageArray[pageIndex].appearence;

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

        components.behavioursText.text = behaveText;
        components.appearenceText.text = "- " + NPCTraits.appearenceDescriptions[appearPosition];

        components.borderImage.color = soData.clipboardStats.profilePageArray[pageIndex].color;

        components.rectTransform.localPosition = new Vector3(components.rectTransform.localPosition.x, components.rectTransform.localPosition.y, -(soData.clipboardStats.profilePageArray.Length - 1 - pageIndex));
    }
    public void Flipped(bool flippedDown)
    {
        components.borderImage.enabled = flippedDown;
        components.behavioursText.enabled = flippedDown;
        components.appearenceText.enabled = flippedDown;
    }
}
