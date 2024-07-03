using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;

public class TeleportationClick : MonoBehaviour
{
    [SerializeField] private GameObject rightHandTeleportInteractor; // Assign your Teleportation Interactor here
    [SerializeField] private TeleportationProvider teleportationProvider; // Assign your Teleportation Provider here
    [SerializeField] private PressableButtonHoloLens2 teleportButton; // Assign your PressableButtonHoloLens2 here

    private void Start()
    {
        // Subscribe to the press event of the PressableButtonHololens2
        if (teleportButton != null)
        {
            teleportButton.ButtonPressed.AddListener(OnTeleportButtonClicked);
        }
    }

    private void OnDestroy()
    {
        // Make sure to unsubscribe from events to prevent memory leaks
        if (teleportButton != null)
        {
            teleportButton.ButtonPressed.RemoveListener(OnTeleportButtonClicked);
        }
    }

    public void OnTeleportButtonClicked()
    {
        bool isActive = rightHandTeleportInteractor.activeSelf;
        rightHandTeleportInteractor.SetActive(!isActive);

        if (!isActive)
        {
            // If the teleportation interactor was turned off, perform the teleportation
            XRRayInteractor rayInteractor = rightHandTeleportInteractor.GetComponent<XRRayInteractor>();
            if (rayInteractor != null && rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                // Queue teleportation request to the hit point
                teleportationProvider.QueueTeleportRequest(new TeleportRequest { destinationPosition = hit.point });
            }
        }
    }
}
