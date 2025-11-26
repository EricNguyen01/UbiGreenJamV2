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
    [field: Tooltip("how many ticks should pass before flood damage is dealt to furnitures within the flood trigger")]
    [field: DisableIf("applyDamageMultAfterTicks", false)]
    public float numberOfTicksToDealDamage { get; private set; } = 2.5f;

    [field: SerializeField]
    public bool allowCumulativeDamageMultiplier { get; private set; } = false;  

    [field: SerializeField]
    [field: Min(1.0f)]
    [field: Tooltip("how many ticks should pass before the damage multiplier below is cumulated")]
    [field: DisableIf("applyDamageMultAfterTicks", false)]
    public float numberOfTicksToApplyDamageMult { get; private set; } = 5.0f;

    [field: SerializeField]
    [field: Min(1.0f)]
    [field: Tooltip("The cumulative damage multiplier after number of ticks have passed. " +
    "For example, each 2 secs, add this mult to the total cumulative mult to apply back to the base damage")]
    [field: DisableIf("applyDamageMultAfterTicks", false)]
    public float damageMultToApplyAfterTicks { get; private set; } = 1.5f;

    // Add more parameters: spawn patterns, triggers, thresholds...
}
