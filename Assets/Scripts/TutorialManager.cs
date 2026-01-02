using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] PhoneSO phone;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] RectTransform[] parts;

    [System.Serializable]
    public struct BehaviourNPCSprites
    {
        public NPCTraits.Behaviours behaviour;
        public Sprite[] sprites;
    }

    [SerializeField]
    private BehaviourNPCSprites[] npcExampleSprites;

    int curIndex = -1;
    int prevIndex = -1;

    float partTimer;
    bool selectedOnAllProfiles;
    private void Awake()
    {
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].gameObject.SetActive(false);
        }
    }
    private void OnEnable()
    {
        gameEventData.OnStartTutorial.RegisterListener(StartTutorial);
    }

    private void OnDisable()
    {
        gameEventData.OnStartTutorial.UnregisterListener(StartTutorial);
    }

    private void Update()
    {
        SetPart();
        UpdatePart();
        partTimer += Time.deltaTime;
    }

    private void SetPart()
    {
        if (curIndex == prevIndex) return;
        ExitPart();
        prevIndex = curIndex;
        EnterPart();
        partTimer = 0;
    }

    private void UpdatePart()
    {

        switch (curIndex)
        {
            case 0:
            {
                if (playerInputs.mouseScroll == 1 && partTimer > phone.minTutorialPartTime)
                {
                    curIndex++;
                }
            }
            break;
            case 1:
            {
                if (playerInputs.mouseScroll == 1 && partTimer > phone.minTutorialPartTime)
                {
                    curIndex++;
                }
            }
            break;
            case 2:
            {
                for (int i = 0; i < clipboardStats.profilePageArray.Length; i++)
                {
                    if (clipboardStats.profilePageArray[i].spySelected)
                    {
                        selectedOnAllProfiles = true;
                    }
                    else
                    {
                        selectedOnAllProfiles = false;
                        break;
                    }
                }
                if (selectedOnAllProfiles && partTimer > phone.minTutorialPartTime)
                {
                    curIndex++;
                }
            }
            break;
            case 3:
            {

            }
            break;
        }
    }

    private void EnterPart()
    {
        switch (curIndex)
        {
            case 0:
            {
                gameEventData.OnGameFadeIn.Raise();
                parts[curIndex].gameObject.SetActive(true);
            }
            break;
            case 1:
            {
                parts[curIndex].gameObject.SetActive(true);
            }
            break;
            case 2:
            {
                gameEventData.OnGameFadeOut.Raise();
                parts[curIndex].gameObject.SetActive(true);
            }
            break;
            case 3:
            {

                parts[curIndex].gameObject.SetActive(true);
            }
            break;
        }
    }

    private void ExitPart()
    {
        switch (curIndex)
        {
            case 0:
            {
                parts[prevIndex].gameObject.SetActive(false);

            }
            break;
            case 1:
            {
                parts[prevIndex].gameObject.SetActive(false);

            }
            break;
            case 2:
            {
                parts[prevIndex].gameObject.SetActive(false);

            }
            break;
            case 3:
            {
                parts[prevIndex].gameObject.SetActive(false);
                gameEventData.OnGameFadeOut.Raise();
                phone.spyOnPhone = false;

            }
            break;
        }
    }
    private void StartTutorial()
    {
        curIndex++;
    }
}
