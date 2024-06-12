using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TextFieldClickHandler : MonoBehaviour, IPointerClickHandler
{
    public GameObject keypad;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerPress.CompareTag("InputField"))
        {
            keypad.SetActive(true);
        }
    }
}