// Assets/Scripts/Interactions/IInteractable.cs
using UnityEngine;

/// <summary>
/// Interface for any object that can be interacted with by the player
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Gets the text to display when the player can interact with this object
    /// </summary>
    string GetInteractionText();

    /// <summary>
    /// Called when the player interacts with this object
    /// </summary>
    /// <param name="interactor">The GameObject that initiated the interaction (usually the player)</param>
    void Interact(GameObject interactor);
}