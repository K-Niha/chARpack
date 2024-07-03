using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class teleportationClick : MonoBehaviour
{
    public XRRayInteractor rayInteractor; // Assign your Teleport Interactor here
    public TeleportationProvider teleportationProvider; // Assign your Teleportation Provider here
    public Button teleportButton; // Assign your UI Button here

    private void OnEnable()
    {
        teleportButton.onClick.AddListener(OnTeleportButtonClicked);
    }

    private void OnDisable()
    {
        teleportButton.onClick.RemoveListener(OnTeleportButtonClicked);
    }

    private void OnTeleportButtonClicked()
    {
        rayInteractor.gameObject.SetActive(!rayInteractor.gameObject.activeSelf);
        if (!rayInteractor.gameObject.activeSelf)
        {
            // If the rayInteractor was turned off, perform the teleportation
            if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                teleportationProvider.QueueTeleportRequest(new TeleportRequest { destinationPosition = hit.point });
            }
        }
    }
}

