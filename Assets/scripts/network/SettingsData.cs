using UnityEngine;

public class SettingsData
{
    [SerializeField] static public ushort bondStiffness = 1;
    [SerializeField] static public float repulsionScale = 0.5f;
    [SerializeField] static public bool forceField = true;
    [SerializeField] static public bool spatialMesh = false;
    [SerializeField] static public bool handMesh = true;
    [SerializeField] static public bool handJoints = false;
    [SerializeField] static public bool handRay = false;
    [SerializeField] static public bool handMenu = true;
    [SerializeField] static public string language = "en";
    [SerializeField] static public bool gazeHighlighting = false;
    [SerializeField] static public bool pointerHighlighting = true;
    [SerializeField] static public bool showAllHighlightsOnClients = true;
    [SerializeField] private static int _highlightColorMap = 0;
    [SerializeField] static public bool rightHandMenu = false;
    [SerializeField] static public ForceField.Method integrationMethod = ForceField.Method.MidPoint;
    [SerializeField] static public float[] timeFactors = new float[] { /*Euler*/0.6f, /*SV*/0.75f, /*RK*/0.25f, /*MP*/0.2f };
    [SerializeField] static public GlobalCtrl.InteractionModes interactionMode = GlobalCtrl.InteractionModes.NORMAL;
    [SerializeField] static public bool[] coop = new bool[] { /*User box*/true, /*User ray*/true };
    [SerializeField] static public bool networkMeasurements = true;
    [SerializeField] static public bool interpolateColors = true;
    [SerializeField] static public bool useAngstrom = true;


    
    public static int highlightColorMap { get => _highlightColorMap; set
        {

            if (StructureFormulaManager.Singleton)
            {
                StructureFormulaManager.Singleton.setColorMap(value);
            }
            _highlightColorMap = value;
        }
    }
}
