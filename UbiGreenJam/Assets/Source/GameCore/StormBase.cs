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
    public float currentStormDamage { get; private set; } = 1.0f;// Use this to get the storm's damage at any point in its duration

    [SerializeField]
    [ReadOnlyInspector]
    private float _elapsed = 0.0f;

    [field: SerializeField]
    [field: ReadOnlyInspector]
    public bool IsFinished { get; private set; } = false;

    private float cumulativeDamMult = 1.0f;

    private float currentDamageTimeTicks = 0.0f;

    private float currentDamageMultTimeTicks = 0.0f;

    public StormBase(ScriptableStormData data)
    {
        _data = data;
        _elapsed = 0f;
        IsFinished = false;

        if (!data)
        {
            Debug.LogError($"StormBase object doesn't have a valid ref to a ScriptableStormData. It won't work correctly!");
        }
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

        ProcessCumulativeDamageMultiplier(delta);

        //This func below only run if storm damage type is set to "PerTick"
        ProcessFloodDamagePerTick(delta);

        if (_elapsed >= _data.duration)
        {
            IsFinished = true;
        }
    }

    public void Reset()
    {
        _elapsed = 0f;

        IsFinished = false;

        currentStormDamage = _data.damagePerTick;

        cumulativeDamMult = 1.0f;

        currentDamageTimeTicks = 0.0f;

        currentDamageMultTimeTicks = 0.0f;
    }

    private void ProcessFloodDamagePerTick(float delta)
    {
        if (currentDamageTimeTicks < _data.numberOfTicksToDealDamage)
        {
            currentDamageTimeTicks += delta;
        }

        if(currentDamageTimeTicks >= _data.numberOfTicksToDealDamage)
        {
            currentStormDamage *= (_data.allowCumulativeDamageMultiplier ? cumulativeDamMult : 1.0f);

            if (currentStormDamage > 99999999.0f) currentStormDamage = 99999999.0f;

            if (FloodController.FloodControllerInstance)
            {
                FloodController.FloodControllerInstance.DamageInteractablesInFloodTrigger();
            }

            currentDamageTimeTicks = 0.0f;
        }
    }

    private void ProcessCumulativeDamageMultiplier(float delta)
    {
        if (!_data.allowCumulativeDamageMultiplier) return;

        if (currentDamageMultTimeTicks < _data.numberOfTicksToApplyDamageMult)
        {
            currentDamageMultTimeTicks += delta;
        }

        if (currentDamageMultTimeTicks >= _data.numberOfTicksToApplyDamageMult)
        {
            cumulativeDamMult += _data.damageMultToApplyAfterTicks;

            currentDamageMultTimeTicks = 0.0f;
        }
    }
}
