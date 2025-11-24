using UnityEngine;

public interface IInteractable
{
    public InteractableBase GetInteractable();
    public bool OnInteractBy(CharacterBase characterInteracted);

    public void OnReleaseBy(CharacterBase characterInteracted);
}
