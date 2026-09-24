using Proselyte.Sigils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

using static Passenger;
public class PassengerManager : MonoBehaviour
{
    public PassengersData passengerData;

    
    public AtlasSO glyphAtlas;
    public Options options;
    public ActionData actionData;

    public TextAsset namesJSON;
    public Texture2D diagonalTexture;

    [Header("Generated")]
    public NameData nameData;
    public bool npcFindingChair;
    public int totalAgentCount;


    private void OnEnable()
    {
        actionData.onBeginTrip += InitPoolsDict;
        actionData.onBeginTrip += CreateNPCProfiles;
    }
    private void OnDisable()
    {
        actionData.onBeginTrip -= InitPoolsDict;
        actionData.onBeginTrip -= CreateNPCProfiles;
    }
    private void Start()
    {
        GraffitiPool = new Graffiti[MAX_GRAFFITI_RENDERERS];
        graffitiActiveCount = -1;
        passengerData.habitDataDict = SetBehaviourContextDictionary(passengerData.habitDataArray);
    }
    public static Graffiti GetGraffitiRenderer(Graffiti graffitiPrefab)
    {
        Graffiti graffitInstance;

        if (graffitiActiveCount < 0)
        {
            graffitInstance = Instantiate(graffitiPrefab);

        }
        else
        {
            graffitInstance = GraffitiPool[graffitiActiveCount];
            graffitiActiveCount--;
        }

        return graffitInstance;
    }
    public static void ReturnGraffiti(Graffiti graffiti)
    {
        if (graffitiActiveCount == MAX_GRAFFITI_RENDERERS - 1) return;

        graffitiActiveCount++;
        GraffitiPool[graffitiActiveCount] = graffiti;
    }
    public static VisualEffect GetGlyph(VisualEffect glyphPrefab, Transform parent)
    {
        if (!GlyphPoolDict.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            GlyphPoolDict[glyphPrefab] = queue;
        }

        if (queue.Count > 0)
        {
            VisualEffect glyphInstance = queue.Dequeue();
            glyphInstance.gameObject.SetActive(true);
            glyphInstance.Reinit();
            glyphInstance.Stop();
            glyphInstance.gameObject.transform.parent = parent;
            return glyphInstance;
        }

        VisualEffect newVisualEffect = Instantiate(glyphPrefab, parent);
        newVisualEffect.Reinit();
        newVisualEffect.Stop();
        return newVisualEffect;
    }
    public static void ReturnGlyph(VisualEffect glyphPrefab, VisualEffect glyphInstance)
    {
        glyphInstance.Stop();
        glyphInstance.gameObject.transform.parent = null;
        if (!GlyphPoolDict.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            GlyphPoolDict[glyphPrefab] = queue;
        }

        queue.Enqueue(glyphInstance);
    }
    public static PassengerBrain GetNPC(PassengerBrain npcPrefab, Vector3 localPos, Transform parent)
    {
        if (!PassengerPoolDict.TryGetValue(npcPrefab, out Queue<PassengerBrain> queue))
        {
            queue = new Queue<PassengerBrain>();
            PassengerPoolDict.Add(npcPrefab, queue);
        }

        if (queue.Count > 0)
        {
            PassengerBrain npc = queue.Dequeue();
            npc.gameObject.SetActive(true);
            npc.gameObject.transform.parent = parent;
            npc.transform.localPosition = localPos;
            return npc;
        }
        PassengerBrain newNPC = Instantiate(npcPrefab, parent);
        newNPC.transform.localPosition = localPos;

        return newNPC;
    }
    public static void ReturnNPC(PassengerBrain npcPrefab, PassengerBrain npcInstance)
    {
        npcInstance.gameObject.transform.parent = null;
        if (!PassengerPoolDict.TryGetValue(npcPrefab, out Queue<PassengerBrain> queue))
        {
            queue = new Queue<PassengerBrain>();
            PassengerPoolDict.Add(npcPrefab, queue);
        }
        queue.Enqueue(npcInstance);
        npcInstance.gameObject.SetActive(false);
    }
    private void InitPoolsDict()
    {
        if (PassengerPoolDict != null)
        {
            PassengerPoolDict.Clear();
        }
        else
        {
            PassengerPoolDict = new Dictionary<PassengerBrain, Queue<PassengerBrain>>();
        }

        if (GlyphPoolDict != null)
        {
            GlyphPoolDict.Clear();
        }
        else
        {
            GlyphPoolDict = new Dictionary<VisualEffect, Queue<VisualEffect>>();
        }
    }

    private void CreateNPCProfiles()
    {
        nameData = JsonUtility.FromJson<NameData>(namesJSON.text);

        List<PassengerProfile> totalNPCProfiles = new List<PassengerProfile>();
        List<PassengerProfile> bystanderProfiles = new List<PassengerProfile>();

        for (int i = 0; i < options.curTrip.passengers.Length; i++)
        {
            PassengerData passenger = options.curTrip.passengers[i];

            int behaviourValue = (int)passenger.behaviours;

            int[] validFlags = new int[HABIT_COUNT];
            int flagCount = 0;

            for (int j = 0; j < HABIT_COUNT; j++)
            {
                int flag = 1 << j;
                if ((behaviourValue & flag) != 0)
                {
                    validFlags[flagCount] = flag;
                    flagCount++;
                }
            }
            for (int j = 0; j < flagCount; j++)
            {
                Habits firstHabit = (Habits)validFlags[j];
                for (int k = j + 1; k < flagCount; k++)
                {
                    Habits secondHabit = (Habits)validFlags[k];
                    Habits twoHabits = firstHabit | secondHabit;

                    PassengerProfile npcProfile = new PassengerProfile
                    {
                        habits = twoHabits,
                        npcPrefabIndex = i,
                    };
                    totalNPCProfiles.Add(npcProfile);
                }
            }
        }

        int totalTraitorsInTrip = 0;

        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = options.curTrip.stationsDataArray[i];
            totalTraitorsInTrip += station.traitorSpawnCount;
        }
        options.curTrip.traitorProfiles = new TraitorProfile[totalTraitorsInTrip]; ;

        int traitorIndex = 0;
        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = options.curTrip.stationsDataArray[i];

            for (int j = 0; j < station.traitorSpawnCount; j++)
            {
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
                PassengerProfile traitorProfile = totalNPCProfiles[randProfileIndex];
                traitorProfile.boardingStationIndex = i;

                int stationsLeft = options.curTrip.stationsDataArray.Length - i;
                float normSpawnIndex = UnityEngine.Random.Range(0, stationsLeft + 1) / (float)stationsLeft;
                float gaussianNormSpawnIndex = Curves.NormalGaussianValue(normSpawnIndex);
                traitorProfile.disembarkingStationIndex = Mathf.Min(i + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft) + 1, options.curTrip.stationsDataArray.Length - 1);

                PassengerData traitor = options.curTrip.passengers[traitorProfile.npcPrefabIndex];

                string name = GenerateName(traitor.gender, traitor.ethnicity);
                options.curTrip.traitorProfiles[traitorIndex] = new TraitorProfile()
                {
                    passengerProfile = traitorProfile,
                    mugShotIndex = traitor.mugShotIndex,
                    fullName = name,
                };

                totalNPCProfiles.RemoveAt(randProfileIndex);

                for (int k = totalNPCProfiles.Count - 1; k >= 0; k--)
                {
                    if (totalNPCProfiles[k].npcPrefabIndex != traitorProfile.npcPrefabIndex) continue;

                    bystanderProfiles.Add(totalNPCProfiles[k]);
                    totalNPCProfiles.RemoveAt(k);
                }

                traitorIndex++;
            }
        }

        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = options.curTrip.stationsDataArray[i];
            station.accompliceProfiles = new PassengerProfile[station.accompliceSpawnCount];

            for (int j = 0; j < station.accompliceSpawnCount; j++)
            {
                int randPrefabIndex = UnityEngine.Random.Range(0, options.curTrip.passengers.Length);
                PassengerProfile accompliceProfile = new PassengerProfile();

                accompliceProfile.npcPrefabIndex = randPrefabIndex;
                accompliceProfile.boardingStationIndex = i;
                accompliceProfile.disembarkingStationIndex = options.curTrip.stationsDataArray.Length - 1;

                station.accompliceProfiles[j] = accompliceProfile;
            }

        }

        totalNPCProfiles.AddRange(bystanderProfiles);
        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = options.curTrip.stationsDataArray[i];

            station.bystanderProfiles = new PassengerProfile[station.bystanderSpawnCount];

            for (int j = 0; j < station.bystanderSpawnCount; j++)
            {
                int randIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
                PassengerProfile bystanderProfile = totalNPCProfiles[randIndex];

                bystanderProfile.boardingStationIndex = i;

                int stationsLeft = options.curTrip.stationsDataArray.Length - i;
                float normSpawnIndex = (float)j / (float)station.bystanderSpawnCount;
                float gaussianNormSpawnIndex = Curves.NormalGaussianValue(normSpawnIndex);
                bystanderProfile.disembarkingStationIndex = Mathf.Min(i + 1 + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft), options.curTrip.stationsDataArray.Length - 1);

                station.bystanderProfiles[j] = bystanderProfile;
            }
        }

        actionData.onCreatedPassengerProfiles?.Invoke();
    }
    private string GenerateName(Gender gender, Ethnicity ethnicity)
    {
        string genderString = gender.ToString();
        string ethnicityString = ethnicity.ToString();
        List<FirstName> firstNamesList = new List<FirstName>();

        for (int i = 0; i < nameData.firstNames.Length; i++)
        {
            FirstName fn = nameData.firstNames[i];
            if (fn.gender.Equals(genderString, StringComparison.OrdinalIgnoreCase) &&
                fn.ethnicity.Equals(ethnicityString, StringComparison.OrdinalIgnoreCase))
            {
                firstNamesList.Add(fn);
            }
        }
        if (firstNamesList.Count == 0) return "NoFirstName";

        int firstNameIndex = UnityEngine.Random.Range(0, firstNamesList.Count);
        string firstName = firstNamesList[firstNameIndex].name;

        List<LastName> lastNameList = new List<LastName>();
        for (int i = 0; i < nameData.lastNames.Length; i++)
        {
            LastName ln = nameData.lastNames[i];
            if (ln.ethnicity.Equals(ethnicityString, StringComparison.OrdinalIgnoreCase))
            {
                lastNameList.Add(ln);
            }
        }
        if (lastNameList.Count == 0) return firstName;

        int lastNameIndex = UnityEngine.Random.Range(0, lastNameList.Count);
        string lastName = lastNameList[lastNameIndex].name;

        return firstName + " " + lastName;
    }
}

