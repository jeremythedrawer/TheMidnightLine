using System;
using UnityEngine;

public static class NPCTraits
{
    static NPCTraits() { InitializeDescriptions(); }

    [Flags]
    public enum Appearence
    {
        Nothing = 0,
        White_hair = 1 << 0,
        Blue_collar_worker = 1 << 1,
        Has_a_cain = 1 << 2,
        Near_sighted = 1 << 3,
        Suit_and_tie = 1 << 4,
        Is_bald = 1 << 5,
        Wearing_a_dress = 1 << 7,
        Wears_shorts = 1 << 8,
        Carries_a_bag = 1 << 9,
        Wears_a_hat = 1 << 10,
    }

    [Flags]
    public enum Behaviours
    {
        Nothing = 0,
        Frequent_smoker = 1 << 0,
        Keeps_to_themselves = 1 << 1, // Replace
        Always_checks_the_time = 1 << 2,
        Takes_naps = 1 << 3,
        Prefers_to_stand = 1 << 4, // Replace
        Gets_motion_sick = 1 << 5,
        Always_hungry = 1 << 6,
        Listens_to_heavy_metal = 1 << 7,
        Addicted_to_phone = 1 << 8,
        Reads_books = 1 << 9,
    }

    public static string[] appearenceDescriptions;
    public static string[] behaviourDescriptions;

    public static Behaviours GetBehaviours()
    {
        int numBehaviours = Enum.GetValues(typeof(Behaviours)).Length - 1;

        int firstBehave = 1 << UnityEngine.Random.Range(0, numBehaviours);

        int secondBehave;
        do
        {
            secondBehave = 1 << (UnityEngine.Random.Range(0, numBehaviours));
        }
        while (secondBehave == firstBehave);

        return (Behaviours)(firstBehave | secondBehave);
    }

    public static Appearence GetRandomAppearence(Appearence appearence)
    {
        int appearValue = (int)appearence;

        int[] flags = new int[32];
        int flagCount = 0;

        for (int i = 0; i < flags.Length; i++)
        {
            int flag = 1 << i;
            if ((appearValue & flag) != 0)
            {
                flags[flagCount] = flag;
                flagCount++;
            }
        }

        int chosenFlag = flags[UnityEngine.Random.Range(0, flagCount)];
        return (Appearence)chosenFlag;
    }

    public static void InitializeDescriptions()
    {
        Array appearArray = Enum.GetValues(typeof(Appearence));
        appearenceDescriptions = new string[appearArray.Length - 1];
        int i = 0;
        foreach (Appearence flag in appearArray)
        {
            if (flag == Appearence.Nothing) continue;
            appearenceDescriptions[i] = flag.ToString().Replace("_", " ");
            i++;
        }

        Array behaveArray = Enum.GetValues(typeof(Behaviours));
        behaviourDescriptions = new string[behaveArray.Length - 1];
        i = 0;
        foreach (Behaviours flag in behaveArray)
        {
            if (flag == Behaviours.Nothing) continue;
            behaviourDescriptions[i] = flag.ToString().Replace("_", " ");
            i++;
        }
    }
}

