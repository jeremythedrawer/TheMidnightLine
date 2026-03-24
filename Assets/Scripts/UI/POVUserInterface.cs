using UnityEngine;

public class POVUserInterface : MonoBehaviour
{
    public PlayerInputsSO playerInputs;
    public CameraStatsSO cameraStats;
    public Notepad notepad;
    public Transform background;
    public float backgroundMoveDamp = 5;
    public float notepadMoveDamp = 4;


    public Vector3 backgroundActivePos;
    public Vector3 backgroundInactivePos;

    public Vector3 notepadActivePos;
    public Vector3 notepadInactivePos;
    public bool notpadActive;
    private void Start()
    {
        background.gameObject.SetActive(true);
        notepad.gameObject.SetActive(true);

        backgroundActivePos = background.localPosition;
        notepadActivePos = notepad.transform.localPosition;

        backgroundInactivePos = new Vector3(cameraStats.camWidth * 0.5f, background.localPosition.y, background.localPosition.z);
        notepadInactivePos = new Vector3(cameraStats.camWidth * 0.5f, notepad.transform.localPosition.y, notepad.transform.localPosition.z);
        
        background.localPosition = backgroundInactivePos;
        notepad.transform.localPosition = notepadInactivePos;
    }
    private void Update()
    {
        if (playerInputs.notepad.x == -1 || notpadActive)
        {
            background.localPosition = Vector3.Lerp(background.localPosition, backgroundActivePos, Time.deltaTime * backgroundMoveDamp);
            notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadActivePos, Time.deltaTime * notepadMoveDamp);
            notpadActive = true;
        }

        if (playerInputs.notepad.x == 1 || !notpadActive)
        {
            background.localPosition = Vector3.Lerp(background.localPosition, backgroundInactivePos, Time.deltaTime * backgroundMoveDamp);
            notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadInactivePos, Time.deltaTime * notepadMoveDamp);
            notpadActive = false;

        }
    }
}
