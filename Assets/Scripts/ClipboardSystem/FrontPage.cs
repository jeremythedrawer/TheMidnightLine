using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FrontPage : MonoBehaviour
{
    [SerializeField] TMP_Text idText;
    [SerializeField] Image pageImage;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] TutorialSO tutorial;
    public RectTransform rectTransform;
    public Image flipPageImage;
    public Image idBorderImage;
    private void Awake()
    {
        flipPageImage.material = Instantiate(flipPageImage.material);
        flipPageImage.enabled = false;
        idText.material = Instantiate(idText.material);
        idText.fontMaterial.SetColor("_FaceColor", Color.white);
        idText.text = spyStats.spyID;
    }

    private void Update()
    {
        if (!clipboardStats.tempStats.canClickID) return;
        bool hovered = RectTransformUtility.RectangleContainsScreenPoint(idBorderImage.rectTransform, playerInputs.mouseScreenPos, Camera.main);
        bool clicked = hovered && playerInputs.mouseLeftDown;

        if (hovered)
        {
            if (clicked)
            {
                idBorderImage.material.SetColor("_Color", Color.black);
                clipboardStats.tempStats.canClickID = false;
                tutorial.curConvoIndex++;

            }
            else
            {
                idBorderImage.material.SetColor("_Color", Color.grey); //TODO: Better hover/click feedback
            }
        }
        else
        {
            idBorderImage.material.SetColor("_Color", Color.black);
        }
    }
    public void Flipped(bool flippedDown)
    {
        flipPageImage.enabled = !flippedDown;
        pageImage.enabled = flippedDown;
        idText.enabled = flippedDown;
    }
}
