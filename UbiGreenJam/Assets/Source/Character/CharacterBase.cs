using UnityEngine;

[DisallowMultipleComponent]
public abstract class CharacterBase : MonoBehaviour
{
    [Header("Character SO Data")]

    [SerializeField]
    public CharacterSOBase characterSOData;

    [field: Header("Character Animation")]

    [field: SerializeField] public Animator characterAnimator { get; protected set; }

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

    public void SetAnimatorTrigger(string triggerName)
    {
        if (!characterAnimator) return;

        characterAnimator.SetTrigger(triggerName);
    }

    public void SetAnimatorBool(string boolName, bool boolState)
    {
        if (!characterAnimator) return;

        characterAnimator.SetBool(boolName, boolState);
    }

    public void SetAnimatorLayerWeight(string layer, float weight)
    {
        if (!characterAnimator) return;

        int upperBodyIndex = characterAnimator.GetLayerIndex(layer);

        characterAnimator.SetLayerWeight(upperBodyIndex, weight);
    }
}
