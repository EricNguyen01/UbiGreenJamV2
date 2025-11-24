using UnityEngine;

[DisallowMultipleComponent]
public abstract class CharacterBase : MonoBehaviour
{
    [Header("Character SO Data")]

    [SerializeField]
    public CharacterSOBase characterSOData;

    [field: Header("Character Runtime Data")]

    protected virtual void Awake()
    {
        if (!characterSOData)
        {
            Debug.LogError($"Fatal Error: Character {name} is missing its Character SO Data. " +
                           "Character won't work! Disabling character...");

            gameObject.SetActive(false);

            enabled = false;

            return;
        }

        characterSOData = Instantiate(characterSOData);
    }
}
