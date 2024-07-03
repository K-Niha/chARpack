using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OnClickAppear : MonoBehaviour, IPointerClickHandler
{
    public GameObject gameObjectToAppear;

    // This function is called when the user clicks on the Input Field
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameObjectToAppear != null)
        {
            gameObjectToAppear.SetActive(true); // Make the GameObject appear
        }
    }
}