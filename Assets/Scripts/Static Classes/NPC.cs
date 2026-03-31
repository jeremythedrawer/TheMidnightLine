using System;
using System.Collections.Generic;
using UnityEngine;

public static class NPC
{
    public enum NPCState
    {
        Idling,
        Walking,
        TicketCheck,
        Smoking,
        Sleeping,
        Eating,
        Music,
        Calling,
        Reading
    }

    public enum Role
    {
        Traitor,
        Bystander
    }

    public enum Path
    {
        None,
        ToSmokerRoom,
        ToChair,
        ToSlideDoor,
    }
    [Flags] public enum Appearance
    {
        None = 0,
        White_hair = 1 << 0,
        Blue_collar_worker = 1 << 1,
        Has_a_cain = 1 << 2,
        Near_sighted = 1 << 3,
        Suit_and_tie = 1 << 4,
        Is_bald = 1 << 5,
        Big_boned = 1 << 6,
        Wearing_a_dress = 1 << 7,
        Wears_shorts = 1 << 8,
        Carries_a_bag = 1 << 9,
        Wears_a_hat = 1 << 10,
        Wears_a_necklace = 1 << 11,
    }


    [Flags] public enum Behaviours
    {
        None = 0,
        Frequent_smoker = 1 << 0,
        Takes_naps = 1 << 2,
        Always_hungry = 1 << 3,
        Listens_to_music = 1 << 4,
        Lots_of_phone_calls = 1 << 5,
        Enjoys_reading = 1 << 6,
    }

    public enum Gender
    { 
        Male,
        Female,
    }

    public enum Ethnicity
    {
        European,
        Western,
    }

    [Serializable] public struct NPCProfile
    {
        public string fullName;

        public int departureStationIndex;
        public int arrivalStationIndex;

        public int npcPrefabIndex;

        public Behaviours behaviours;
        public Appearance appearence;
    }

    [Serializable] public class NameData
    {
        public FirstName[] firstNames;
        public LastName[] lastNames;
    }
    [Serializable] public struct FirstName
    {
        public string gender;
        public string ethnicity;
        public string name;
    }
    [Serializable] public struct LastName
    {
        public string ethnicity;
        public string name;
    }
}

