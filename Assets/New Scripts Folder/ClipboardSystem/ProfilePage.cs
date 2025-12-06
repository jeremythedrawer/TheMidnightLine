using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class ProfilePage : MonoBehaviour
{
    [Serializable] public struct ComponentData
    {
        public TMP_Text behaviours;
        public TMP_Text appearences;
    }
    [SerializeField] ComponentData components;

    [Serializable] public struct SOData
    {
        public ClipboardStatsSO clipboardStats;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct Stats
    {
        internal int pageIndex;
    }
    public Stats stats;

    private void OnEnable()
    {
        NPCTraits.Behaviours rawBehave = soData.clipboardStats.profilePageData[stats.pageIndex].behaviours;
        NPCTraits.Appearence rawAppear = soData.clipboardStats.profilePageData[stats.pageIndex].appearence;

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
        foreach(NPCTraits.Appearence flag in Enum.GetValues(typeof (NPCTraits.Appearence)))
        {
            if (flag == NPCTraits.Appearence.Nothing) continue;

            if (rawAppear.HasFlag(flag))
            {
                appearPosition = flagIndex;
                break;
            }
            flagIndex++;
        }

        components.behaviours.text = "- " + NPCTraits.behaviourDescriptions[behavePositions[0]] + Environment.NewLine + "- " + NPCTraits.behaviourDescriptions[behavePositions[1]];
        components.appearences.text = "- " + NPCTraits.appearenceDescriptions[appearPosition];

    }
}
