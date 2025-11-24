using UnityEngine;

public abstract class CharacterComponentBase : MonoBehaviour
{
    [Header("Character Component Base Data")]

    [SerializeField]
    [ReadOnlyInspector]
    protected CharacterBase characterUsingComponent;

    protected virtual void Start()
    {
        if (!characterUsingComponent || !characterUsingComponent.characterSOData)
        {
            Debug.LogError($"Character Component {name} doesn't have a valid ref to a Character or its referenced Character doesn't have any Character SO Data. " +
                           "Disabling character component...");

            enabled = false;
        }
    }

    public virtual bool InitCharacterComponentFrom(CharacterBase character)
    {
        if (!character || !character.characterSOData)
        {
            enabled = false;

            return false;
        }

        characterUsingComponent = character;

        return true;
    }
}
