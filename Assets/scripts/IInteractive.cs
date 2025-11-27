public interface IInteractive
{
    bool IsInteractive { get; }
    void OnCursorEnter();
    void OnCursorExit();
}