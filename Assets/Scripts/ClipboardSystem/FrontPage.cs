using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FrontPage : MonoBehaviour
{
    [SerializeField] TMP_Text idText;
    [SerializeField] Image pageImage;
    [SerializeField] SpyStatsSO spyStats;
    public RectTransform rectTransform;
    public Image flipPageImage;

    private void Awake()
    {
        flipPageImage.material = Instantiate(flipPageImage.material);
        flipPageImage.enabled = false;
        idText.text = spyStats.spyID;
    }
    public void Flipped(bool flippedDown)
    {
        flipPageImage.enabled = !flippedDown;
        pageImage.enabled = flippedDown;
        idText.enabled = flippedDown;
    }
}
