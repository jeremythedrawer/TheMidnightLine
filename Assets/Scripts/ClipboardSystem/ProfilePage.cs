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

        int[] behavePositions = new int[2];
        int appearPosition = 0;

        int posIndex = 0;
        int flagIndex = 0;
        foreach (NPCTraits.Behaviours flag in Enum.GetValues(typeof(NPCTraits.Behaviours)))
        {
            if (flag == NPCTraits.Behaviours.Nothing) continue;

            if (rawBehave.HasFlag(flag))
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

            if (rawAppear.HasFlag(flag))
            {
                appearPosition = flagIndex;
                break;
            }
            flagIndex++;
        }

        components.behavioursText.text = "- " + NPCTraits.behaviourDescriptions[behavePositions[0]] + Environment.NewLine + "- " + NPCTraits.behaviourDescriptions[behavePositions[1]];
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
