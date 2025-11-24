using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[CreateAssetMenu(fileName = "CharacterSOBase", menuName = "Scriptable Objects/CharacterSOBase")]
public class CharacterSOBase : ScriptableObject
{
    [field: Header("Character General Data")]

    [field: SerializeField]
    public bool isAICharacter { get; private set; } = false;

    [field: Header("Character Stats")]

    [field: SerializeField]
    public float health { get; set; } = 100.0f;

    [field: SerializeField]
    public float stamina { get; set; } = 100.0f;

    [field: SerializeField]
    public float fixSpeed { get; set; } = 5.0f;

    [field: Header("Character Movement Data")]

    [field: SerializeField]
    public float speed { get; set; } = 12.0f;

    [field: SerializeField]
    public float jumpHeight { get; set; } = 4.0f;

    // How much control the player has while in the air (1 = full control, 0 = no control)
    [field: SerializeField]
    public float airControlMultiplier { get; private set; } = 0.5f;

    // How quickly horizontal velocity approaches the target while in the air
    [field: SerializeField]
    public float airAcceleration { get; private set; } = 10f;

    [field: SerializeField]
    public float gravity { get; private set; } = -20.0f;

    [field: Header("Character Mouse Look Data")]

    [field: SerializeField]
    public float mouseHorizontalSensitivity { get; private set; } = 450.0f;

    [field: SerializeField]
    public float mouseVerticalSensitivity { get; private set; } = 800.0f;
}
