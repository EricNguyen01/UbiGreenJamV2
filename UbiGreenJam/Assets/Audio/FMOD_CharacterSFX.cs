using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FMOD_CharacterSFX : MonoBehaviour
{
    private int MaterialValue;
    private RaycastHit rh;
    private float distance = 0.3f;
    private PARAMETER_ID ParamID;
    private PARAMETER_ID ParamID2;
    private LayerMask lm;
    /*
    private string EventPathFootsteps = "event:/Footsteps";
    private string EventPathLanded = "event:/LandGround";
    private string EventPathJump = "event:/LandGround";
    */

    [field: Header("Footsteps")]
    [field: SerializeField] public EventReference FootstepsSFX { get; private set; }

    [field: Header("Landed")]
    [field: SerializeField] public EventReference LandedSFX { get; private set; }

    [field: Header("Jump")]
    [field: SerializeField] public EventReference JumpSFX { get; private set; }

    //Section Used for finding FMOD parameter ID number (instead of name)
    /*
    private EventDescription EventDes;
    private PARAMETER_DESCRIPTION ParamDes;
    */

    private void Start()
    {
        // This section returns the parameter ID in the console
        
        /* EventDes = RuntimeManager.GetEventDescription(EventPath);
        EventDes.getParameterDescriptionByName("Terrain", out ParamDes);
        ParamID = ParamDes.id;
        Debug.Log(ParamID.data1 + " " + ParamID.data2);*/
        

        // Assigns the ID of 'Terrain'
        ParamID.data1 = 1082362436;
        ParamID.data2 = 3768982689;

        lm = LayerMask.GetMask("Ground");        
    }

    /*
    void Update()
    {
        // Shows drawn raycast for debugging
        Debug.DrawRay(transform.position, Vector3.down * distance, Color.blue);
    }
    */


    void PlayWalkEvent()
    {
        // Start with material check then instantiate sound
        MaterialCheck();
        EventInstance Walk = RuntimeManager.CreateInstance(FootstepsSFX);
        RuntimeManager.AttachInstanceToGameObject(Walk, transform.gameObject, GetComponent<Rigidbody>());

        // Sets the Terrain parameter
        Walk.setParameterByID(ParamID, MaterialValue, false);

        // Can be used as alternative to IDs
        // Run.setParameterByName("Terrain", MaterialValue);

        Walk.start();
        Walk.release();
    }

    void PlayLandedEvent() 
    {
        EventInstance Landed = RuntimeManager.CreateInstance(LandedSFX);
        RuntimeManager.AttachInstanceToGameObject(Landed, transform.gameObject, GetComponent<Rigidbody>());

        // Sets the Terrain parameter
        Landed.setParameterByID(ParamID, MaterialValue, false);
        // Can be used as alternative to IDs
        // Run.setParameterByName("Terrain", MaterialValue);

        Landed.start();
        Landed.release();

    }

    void PlayJumpEvent()
    {
        EventInstance Jump = RuntimeManager.CreateInstance(JumpSFX);
        RuntimeManager.AttachInstanceToGameObject(Jump, transform.gameObject, GetComponent<Rigidbody>());

        // Sets the Terrain parameter
        Jump.setParameterByID(ParamID, MaterialValue, false);

        // Can be used as alternative to IDs
        // Run.setParameterByName("Terrain", MaterialValue);

        Jump.start();
        Jump.release();

    }

    // Sets parameter based on RaycastHit
    void MaterialCheck()
    {

        if (Physics.Raycast(transform.position, Vector3.down, out rh, distance, lm))
        {
            //Debug.Log(rh.collider.tag + " " + MaterialValue);
            switch (rh.collider.tag)
            {
                case "Ground":
                    MaterialValue = 0; // Labeled parameters in FMOD
                    break;
                case "Water":
                    MaterialValue = 1;
                    break;

            }
        }
    }
}