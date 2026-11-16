namespace MistIsle.World
{
    public interface IWorldInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract { get; }
        void Interact();
    }
}
