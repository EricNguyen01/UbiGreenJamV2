using UnityEngine;

[CreateAssetMenu(menuName = "Game/Interactables/Item Database")]
public class InteractableItemDatabase : ScriptableObject
{
    public InteractableItemData[] items;

    public InteractableItemData GetItem(InteractableItemType type)
    {
        foreach (var itm in items)
        {
            if (itm.itemType == type)
                return itm;
        }
        return null;
    }
}
