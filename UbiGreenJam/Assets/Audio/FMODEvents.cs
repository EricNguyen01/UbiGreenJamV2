using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour 
{
    [field: Header("UI Promt Popup")]
    [field: SerializeField] public EventReference UIPopUpSound { get; private set; }
    
    [field: Header("Pickup SFX")]
    [field: SerializeField] public EventReference PickupSFX { get; private set; }

    [field: Header("Ambience Sound")]
    [field: SerializeField] public EventReference AmbienceSound { get; private set; }


    [field: Header("Drain Interaction Sound")]
    [field: SerializeField] public EventReference DrainInteraction { get; private set; }

    [field: Header("Drain Unclogging Sound")]
    [field: SerializeField] public EventReference DrainUnclog { get; private set; }

    [field: Header("Ground Land SFX")]
    [field: SerializeField] public EventReference GroundLand { get; private set; }

    [field: Header("Water Land SFX")]
    [field: SerializeField] public EventReference WaterLand { get; private set; }

    [field: Header("Water Jump SFX")]
    [field: SerializeField] public EventReference WaterJump { get; private set; }

    [field: Header("Footsteps SFX")]
    [field: SerializeField] public EventReference FootstepsSFX { get; private set; }

    [field: Header("Swimming SFX")]
    [field: SerializeField] public EventReference SwimmingSFX { get; private set; }

    public static FMODEvents Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
            {
            Debug.LogError("Multiple instances of FMODEvents detected!");
        }
        Instance = this;
    }

}
