using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterMovement), typeof(MouseLook))]
public class PlayerCharacter : CharacterBase
{
    [field: Header("Player Character Components")]
    [field: SerializeField] public MouseLook characterMouseLook { get; protected set; }
    [field: SerializeField] public CharacterMovement characterMovement { get; protected set; }

    private Camera playerCamera;
    private AudioListener audioListener;
    private bool isMultiplayer;

    protected override void Awake()
    {
        base.Awake();

        isMultiplayer = PhotonNetwork.InRoom;

        playerCamera = GetComponentInChildren<Camera>(true);
        audioListener = GetComponentInChildren<AudioListener>(true);

        if (!characterMovement)
        {
            characterMovement = GetComponent<CharacterMovement>();
            if (!characterMovement)
            {
                Debug.LogError($"Character {name} is missing its Character Movement component. Adding one...");
                characterMovement = gameObject.AddComponent<CharacterMovement>();
            }
        }
        characterMovement.InitCharacterComponentFrom(this);

        if (!characterMouseLook)
        {
            characterMouseLook = GetComponent<MouseLook>();
            if (!characterMouseLook)
            {
                Debug.LogError($"Character {name} is missing its MouseLook component. Adding one...");
                characterMouseLook = gameObject.AddComponent<MouseLook>();
            }
        }
        characterMouseLook.InitCharacterComponentFrom(this);
    }

    void Start()
    {
        if (isMultiplayer)
        {
            if (TryGetComponent<PhotonView>(out var pv))
            {
                bool isMine = pv.IsMine;

                if (playerCamera != null) playerCamera.enabled = isMine;
                if (audioListener != null) audioListener.enabled = isMine;

                characterMovement.enabled = isMine;
                characterMouseLook.enabled = isMine;
            }
        }
        else
        {
            if (playerCamera != null) playerCamera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;

            characterMovement.enabled = true;
            characterMouseLook.enabled = true;
        }
    }
}
