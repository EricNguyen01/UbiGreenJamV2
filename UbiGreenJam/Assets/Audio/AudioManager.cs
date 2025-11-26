using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

// Explicitly qualify EventReference to resolve CS0433 ambiguity
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Multiple instances of AudioManager detected!");
        }
        Instance = this;
    }

    public void PlayOneShot(EventReference sound, Vector3 position)
    {
        FMODUnity.RuntimeManager.PlayOneShot(sound, position);
    }

}
