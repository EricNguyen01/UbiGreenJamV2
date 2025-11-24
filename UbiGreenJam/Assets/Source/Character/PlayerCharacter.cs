using UnityEngine;

[RequireComponent(typeof(CharacterMovement), typeof(MouseLook))]
public class PlayerCharacter : CharacterBase
{
    [field: Header("Player Character Components")]

    [field: SerializeField]
    public MouseLook characterMouseLook { get; protected set; }

    [field: SerializeField]
    public CharacterMovement characterMovement { get; protected set; }

    protected override void Awake()
    {
        base.Awake();

        if (!characterMovement)
        {
            characterMovement = GetComponent<CharacterMovement>();

            if (!characterMovement)
            {
                Debug.LogError($"Character {name} is missing its Character Movement component. " +
                               "One will be added but the character and its movement might not work correctly!");

                characterMovement = gameObject.AddComponent<CharacterMovement>();
            }
        }

        characterMovement.InitCharacterComponentFrom(this);

        if (!characterMouseLook)
        {
            characterMouseLook = GetComponent<MouseLook>();

            if (!characterMouseLook)
            {
                Debug.LogError($"Character {name} is missing its MouseLook component. " +
                               "One will be added but the character and it might not work correctly!");

                characterMouseLook = gameObject.AddComponent<MouseLook>();
            }
        }

        characterMouseLook.InitCharacterComponentFrom(this);
    }
}
