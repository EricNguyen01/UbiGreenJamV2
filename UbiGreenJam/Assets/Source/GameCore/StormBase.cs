using System;
using UnityEngine;

/// <summary>
/// Runtime controller created from ScriptableStormData
/// Contains runtime logic of the storm (tick, triggers, finished)
/// </summary>
[Serializable]
public class StormBase
{
    [Header("Storm Data")]

    [SerializeField]
    private ScriptableStormData _data;

    [Header("Runtime Storm Data Read Only")]

    [field: SerializeField]
    [field: ReadOnlyInspector]
    private ScriptableStormData.StormDamageType stormDamageType = ScriptableStormData.StormDamageType.PerTick;

    [field: SerializeField]
    [field: ReadOnlyInspector]
    public float currentStormDamage { get; private set; } = 1.0f;// Use this to get the storm's damage at any point in its duration

    [SerializeField]
    [ReadOnlyInspector]
    private float _elapsed = 0.0f;

    [field: SerializeField]
    [field: ReadOnlyInspector]
    public bool IsFinished { get; private set; } = false;

    private float currentDamageTimeTicks = 0.0f;

    public StormBase(ScriptableStormData data)
    {
        _data = data;
        _elapsed = 0f;
        IsFinished = false;

        if (!data)
        {
            Debug.LogError($"StormBase object doesn't have a valid ref to a ScriptableStormData. It won't work correctly!");

            return;
        }

        stormDamageType = data.stormDamageType;

        Reset();
    } 

    public void StartStorm()
    {
        _elapsed = 0f;

        IsFinished = false;
        // optionally spawn initial triggers
    }

    public void Tick(float delta)
    {
        if (IsFinished) return;

        _elapsed += delta;

        // Here you can process triggers, spawn events, use intensity etc.

        //This func below only run if storm damage type is set to "PerTick"
        ProcessDamagePerTick(delta);

        if (_elapsed >= _data.duration)
        {
            IsFinished = true;
        }
    }

    public void Reset()
    {
        _elapsed = 0f;

        IsFinished = false;

        if (stormDamageType == ScriptableStormData.StormDamageType.Fixed)
        {
            currentStormDamage = _data.stormFixedDamage * _data.roundDamageMultiplier;
        }
        else
        {
            currentStormDamage = _data.damagePerTick;
        }

        currentDamageTimeTicks = 0.0f;
    }

    private void ProcessDamagePerTick(float delta)
    {
        if (stormDamageType != ScriptableStormData.StormDamageType.PerTick) return;

        if (currentDamageTimeTicks < _data.numberOfTicksToApplyDamageMult)
        {
            currentDamageTimeTicks += delta;
        }

        if(currentDamageTimeTicks >= _data.numberOfTicksToApplyDamageMult)
        {
            currentStormDamage *= _data.damageMultToApplyAfterTicks * _data.roundDamageMultiplier;

            currentDamageTimeTicks = 0.0f;
        }
    }
}
