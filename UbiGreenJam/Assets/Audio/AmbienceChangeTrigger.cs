using UnityEngine;

public class AmbienceChangeTrigger : MonoBehaviour
{
    [Header("Parameter Change On Enter")]

    [SerializeField] private string parameterNameEnter;
    [SerializeField] private float parameterValueEnter;

    [Header("Parameter Change On Leave")]

    [SerializeField] private string parameterNameLeave;
    [SerializeField] private float parameterValueLeave;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Change the ambience parameter when the player enters the trigger
            //Add the (AudioManager.Instance) to check if the audio manager instance is null or not
            if(AudioManager.Instance) AudioManager.Instance.SetAmbienceParameter(parameterNameEnter, parameterValueEnter);

            Debug.Log($"Player entered trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Optionally reset the parameter when exiting the trigger
            if (AudioManager.Instance) AudioManager.Instance.SetAmbienceParameter(parameterNameLeave, parameterValueLeave);
            Debug.Log($"Player exit trigger");
        }
    }
}
