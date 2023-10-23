using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class appSettings : MonoBehaviour
{

    private static appSettings _singleton;
    public static appSettings Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(appSettings)}] Instance already exists, destroying duplicate!");
                Destroy(value.gameObject);
            }

        }
    }
    private void Awake()
    {
        Singleton = this;
    }

    public GameObject bondStiffnessValueGO;
    public GameObject repuslionScaleValueGO;
    // Indicators
    public GameObject ForceFieldIndicator;
    public GameObject HandJointIndicator;
    public GameObject HandMenuIndicator;
    public GameObject HandMeshIndicator;
    public GameObject HandRayIndicator;
    public GameObject SpatialMeshIndicator;
    public GameObject DebugWindowIndicator;
    public GameObject GazeHighlightingIndicator;
    public GameObject RightHandMenuIndicator;
    public GameObject UserBoxIndicator;
    public GameObject UserRayIndicator;

    private Color orange = new Color(1.0f, 0.5f, 0.0f);

    private void Start()
    {
        updateVisuals();
        try 
        { 
            var userBoxes = GameObject.FindGameObjectsWithTag("User Box");
            setVisual(UserBoxIndicator, true);
            setVisual(UserRayIndicator, true);
        } catch // not in coop mode
        {
            setVisual(UserBoxIndicator, false);
            setVisual(UserRayIndicator, false);
        }
    }

    public void toggleSpatialMesh()
    {
        // Get the first Mesh Observer available, generally we have only one registered
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        if (observer.DisplayOption == SpatialAwarenessMeshDisplayOptions.None)
        {
            // Set to visible
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.Visible;
            SettingsData.spatialMesh = true;
        }
        else
        {
            // Set to not visible
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
            SettingsData.spatialMesh = false;
        }
        updateVisuals();
    }

    public void toggleForceField()
    {
        ForceField.Singleton.toggleForceFieldUI();
        updateVisuals();
    }

    public void toggleDebugWindow()
    {
        GlobalCtrl.Singleton.toggleDebugWindow();
        updateVisuals();
    }

    public void increaseBondStiffness()
    {
        if (ForceField.Singleton.stiffness < 4)
        {
            ForceField.Singleton.stiffness += 1;
            SettingsData.bondStiffness = ForceField.Singleton.stiffness;
            updateVisuals();
        }
    }

    public void decreaseBondStiffness()
    {
        if (ForceField.Singleton.stiffness > 0)
        {
            ForceField.Singleton.stiffness -= 1;
            SettingsData.bondStiffness = ForceField.Singleton.stiffness;
            updateVisuals();
        }
    }


    public void increaseRepusionScale()
    {
        if (ForceField.Singleton.repulsionScale < 0.9f)
        {
            ForceField.Singleton.repulsionScale += 0.1f;
            SettingsData.repulsionScale = ForceField.Singleton.repulsionScale;
            updateVisuals();
        }
    }

    public void decreaseRepusionScale()
    {
        if (ForceField.Singleton.repulsionScale > 0.1f)
        {
            ForceField.Singleton.repulsionScale -= 0.1f;
            SettingsData.repulsionScale = ForceField.Singleton.repulsionScale;
            updateVisuals();
        }
    }

    /// <summary>
    /// Toggles a pointer's "enabled" behavior. If a pointer's is Default or AlwaysOn,
    /// set it to AlwaysOff. Otherwise, set the pointer's behavior to Default.
    /// Will set this state for all matching pointers.
    /// </summary>
    /// <typeparam name="T">Type of pointer to set</typeparam>
    /// <param name="inputType">Input type of pointer to set</param>
    public void TogglePointerEnabled<T>(InputSourceType inputType) where T : class, IMixedRealityPointer
    {
        PointerBehavior oldBehavior = PointerUtils.GetPointerBehavior<T>(Handedness.Any, inputType);
        PointerBehavior newBehavior;
        if (oldBehavior == PointerBehavior.AlwaysOff)
        {
            newBehavior = PointerBehavior.AlwaysOn;
            SettingsData.handRay = true;
        }
        else
        {
            newBehavior = PointerBehavior.AlwaysOff;
            SettingsData.handRay = false;
        }
        PointerUtils.SetPointerBehavior<T>(newBehavior, inputType);
    }

    // Switch languages between German and English
    public void switchLanguage()
    {
        LocaleIdentifier current = LocalizationSettings.SelectedLocale.Identifier;
        if(current == "en")
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("de");
        }
        else
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
        }
    }

    public void toggleGazeHighlighting()
    {
        SettingsData.gazeHighlighting = !SettingsData.gazeHighlighting;
        updateVisuals();
    }


    #region Hand settings
    public void toggleHandSettingsMenu()
    {
        GameObject handSettings = gameObject.transform.Find("HandSettings").gameObject;
        handSettings.SetActive(!handSettings.activeSelf);
    }

    /// <summary>
    /// Toggles hand mesh visualization
    /// </summary>
    public void toggleHandMesh()
    {
        MixedRealityInputSystemProfile inputSystemProfile = CoreServices.InputSystem?.InputSystemProfile;
        if (inputSystemProfile == null)
        {
            return;
        }

        MixedRealityHandTrackingProfile handTrackingProfile = inputSystemProfile.HandTrackingProfile;
        if (handTrackingProfile != null)
        {
            handTrackingProfile.EnableHandMeshVisualization = !handTrackingProfile.EnableHandMeshVisualization;
            SettingsData.handMesh = handTrackingProfile.EnableHandMeshVisualization;
            updateVisuals();
        }
    }

    /// <summary>
    /// Toggles hand joint visualization
    /// </summary>
    public void toggleHandJoint()
    {
        MixedRealityHandTrackingProfile handTrackingProfile = null;

        if (CoreServices.InputSystem?.InputSystemProfile != null)
        {
            handTrackingProfile = CoreServices.InputSystem.InputSystemProfile.HandTrackingProfile;
        }

        if (handTrackingProfile != null)
        {
            handTrackingProfile.EnableHandJointVisualization = !handTrackingProfile.EnableHandJointVisualization;
            SettingsData.handJoints = handTrackingProfile.EnableHandJointVisualization;
            updateVisuals();
        }
    }

    /// <summary>
    /// If hand ray is AlwaysOn or Default, set it to off.
    /// Otherwise, set behavior to default
    /// </summary>
    public void toggleHandRay()
    {
        TogglePointerEnabled<ShellHandRayPointer>(InputSourceType.Hand);
        updateVisuals();
    }

    public void toggleHandMenu()
    {
        GlobalCtrl.Singleton.toggleHandMenu();
        updateVisuals();
    }

    public void toggleMenuHandedness()
    {
        if (handMenu.Singleton.GetComponent<SolverHandler>().TrackedHandedness == Handedness.Left)
        {
            handMenu.Singleton.GetComponent<SolverHandler>().TrackedHandedness = Handedness.Right;
            handMenu.Singleton.setButtonPosition(Handedness.Right);
            SettingsData.rightHandMenu = true;
        }
        else if (handMenu.Singleton.GetComponent<SolverHandler>().TrackedHandedness == Handedness.Right)
        {
            handMenu.Singleton.GetComponent<SolverHandler>().TrackedHandedness = Handedness.Left;
            handMenu.Singleton.setButtonPosition(Handedness.Left);
            SettingsData.rightHandMenu = false;
        }
        updateVisuals();
    }

    #endregion

    #region Cooperation settings

    public void toggleCoopSettings()
    {
        GameObject coopSettings = gameObject.transform.Find("CoopSettings").gameObject;
        coopSettings.SetActive(!coopSettings.activeSelf);
    }

    // TODO: does this have to be broadcast?
    public void toggleUserBox()
    {
        try
        {
            var userBoxes = GameObject.FindGameObjectsWithTag("User Box");
            bool active = userBoxes[0].GetComponent<MeshRenderer>().enabled;
            foreach (GameObject userBox in userBoxes)
            {
                userBox.GetComponent<MeshRenderer>().enabled = !active;
            }
            setVisual(UserBoxIndicator, !active);
        }
        catch 
        {
            setVisual(UserBoxIndicator, false);
        } // No need to do something, we are simply not in coop mode
    }

    public void toggleUserRay()
    {
        try
        {
            var userRays = GameObject.FindGameObjectsWithTag("User Box");
            bool active = userRays[0].GetComponent<LineRenderer>().enabled;
            foreach (GameObject userRay in userRays)
            {
                userRay.GetComponent<LineRenderer>().enabled = !active;
            }
            setVisual(UserRayIndicator, !active);
        } catch
        {
            setVisual(UserRayIndicator, false);
        }
    }

    #endregion

    #region Visuals
    public void updateVisuals()
    {
        setBondStiffnessVisual(SettingsData.bondStiffness);
        setRepulsionScaleVisual(SettingsData.repulsionScale);

        setVisual(HandJointIndicator, SettingsData.handJoints);
        setVisual(HandMenuIndicator, SettingsData.handMenu);
        setVisual(HandMeshIndicator, SettingsData.handMesh);
        setVisual(HandRayIndicator, SettingsData.handRay);
        setVisual(ForceFieldIndicator, SettingsData.forceField);
        setVisual(SpatialMeshIndicator, SettingsData.spatialMesh);

        if (DebugWindow.Singleton == null)
        {
            setVisual(DebugWindowIndicator, false);
        }
        else
        {
            setVisual(DebugWindowIndicator, DebugWindow.Singleton.gameObject.activeSelf);
        }

        setVisual(GazeHighlightingIndicator, SettingsData.gazeHighlighting);
        setVisual(RightHandMenuIndicator, SettingsData.rightHandMenu);
    }

    public void setVisual(GameObject indicator, bool value)
    {
        if (value)
        {
            indicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            indicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    public void setBondStiffnessVisual(ushort value)
    {
        bondStiffnessValueGO.GetComponent<TextMeshPro>().text = value.ToString();
    }

    public void setRepulsionScaleVisual(float value)
    {
        repuslionScaleValueGO.GetComponent<TextMeshPro>().text = value.ToString();
    }
    #endregion
}
