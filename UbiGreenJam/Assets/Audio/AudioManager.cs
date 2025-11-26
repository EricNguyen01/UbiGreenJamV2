using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

// Explicitly qualify EventReference to resolve CS0433 ambiguity
public class AudioManager : MonoBehaviour
{
    private List<EventInstance> eventInstances;
    private EventInstance ambienceEventInstances;
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Multiple instances of AudioManager detected!");
        }
        Instance = this;

        eventInstances = new List<EventInstance>();
    }

    private void Start()
    {
       InitializeAmbience(FMODEvents.Instance.AmbienceSound);
    }

    private void InitializeAmbience(EventReference ambienceEventReference)
    {
        ambienceEventInstances = CreateEventInstance(ambienceEventReference);
        ambienceEventInstances.start();
    }

    public void SetAmbienceParameter(string parameterName, float parameterValue)
    {
        ambienceEventInstances.setParameterByName(parameterName, parameterValue);
    }

    public void PlayOneShot(EventReference sound, Vector3 position)
    {
        FMODUnity.RuntimeManager.PlayOneShot(sound, position);
    }

    public EventInstance CreateEventInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    private void CleanUp()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }

}
