using UnityEngine;

public class PlayerAnimSoundEventTrigger : MonoBehaviour
{
    [SerializeField] private FMOD_CharacterSFX characterFX;

    public void PlayWalkEvent()
    {
        if(!characterFX) return;

        characterFX.PlayWalkEvent();
    }

    public void PlayJumpEvent()
    {
        if (!characterFX) return;

        characterFX.PlayJumpEvent();
    }

    public void PlayLandedEvent()
    {
        if (!characterFX) return;

        characterFX.PlayLandedEvent();
    }
}
