using CrossClimbLite;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/StormData", fileName = "NewStormData")]
public class ScriptableStormData : ScriptableObject
{
    [field: Header("Storm General Data")]

    [field: SerializeField]
    public string stormName { get; private set; } = "DefaultStorm";

    [field: SerializeField]
    [field: Min(1.0f)]
    public float duration { get; private set; } = 10f;

    [field: Header("Storm Damage Settings")]

    [field: SerializeField]
    [field: Min(1.0f)]
    [field: Tooltip("Damage multiplier that will be applied after all other damage modifications. " +
    "Set to value other than 1 if you want to change the overall damage of the storm specifically for a round.")]
    public float roundDamageMultiplier { get; private set; } = 1.0f;

    [field: SerializeField]
    [field: Min(1.0f)]
    public float damagePerTick { get; private set; } = 1.0f;

    [field: SerializeField]
    [field: Min(1.0f)]
    [field: Tooltip("how many ticks should pass before the damage multiplier below is applied")]
    [field: DisableIf("applyDamageMultAfterTicks", false)]
    public float numberOfTicksToApplyDamageMult { get; private set; } = 1.0f;

    [field: SerializeField]
    [field: Min(1.0f)]
    [field: Tooltip("the damage multiplier after number of ticks have passed (if allowed)")]
    [field: DisableIf("applyDamageMultAfterTicks", false)]
    public float damageMultToApplyAfterTicks { get; private set; } = 1.5f;

    // Add more parameters: spawn patterns, triggers, thresholds...
}
