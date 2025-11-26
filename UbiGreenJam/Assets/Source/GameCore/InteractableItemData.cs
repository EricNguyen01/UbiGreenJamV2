using CrossClimbLite;
using UnityEngine;

public enum InteractableItemType
{
    Hammer,
    Plank,
    Mop,
    Flashlight,
    Valuable,
    FurnitureSmall,
    FurnitureBig,
    Pet,
    Bucket
}

[CreateAssetMenu(menuName = "Game/Interactables/Item Data")]
public class InteractableItemData : ScriptableObject
{
    public InteractableItemType itemType;

    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;
    public int startingQuantity = 1;
    public float weight = 0;

    [Header("Gameplay")]
    public bool isCarryable = true;
    public bool isDamageable = true;
    public bool isConsumable = false;
    public bool destroyOnUse = false;

    [Header("Interaction Rules")]
    [TextArea]
    public string interactionDescription;

    [TextArea]
    public string specialNote;

    [Header("Economy / Value")]
    public float health = 100.0f;
    public int cost = 0;
    public bool useCostAsHealth = false;

    [Header("Flood Damage Mitigation")]

    [HelpBox("0.0f = full mitigation, take no damage from flood | 1.0f = no mitigation, take full flood damage.")]
    [Range(0.0f, 1.0f)]
    public float floodDamageMitigation = 1.0f;

}
