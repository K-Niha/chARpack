using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportationClick : MonoBehaviour
{
    public GameObject rightHandTeleportInteractor; // Assign your Teleportation Interactor here
    public TeleportationProvider teleportationProvider; // Assign your Teleportation Provider here

    [SerializeField]
    private InputActionProperty teleportAction; // Reference to the input action for teleportation

    private void OnEnable()
    {
        teleportAction.action.performed += OnTeleportButtonPressed;
        teleportAction.action.Enable();
    }

    private void OnDisable()
    {
        teleportAction.action.performed -= OnTeleportButtonPressed;
        teleportAction.action.Disable();
    }

    private void OnTeleportButtonPressed(InputAction.CallbackContext context)
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
