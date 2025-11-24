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
    public bool isConsumable = false;
    public bool destroyOnUse = false;

    [Header("Interaction Rules")]
    [TextArea]
    public string interactionDescription;

    [TextArea]
    public string specialNote;

    [Header("Economy / Value")]
    public int cost = 0;

}
