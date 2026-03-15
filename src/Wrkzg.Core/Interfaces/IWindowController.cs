namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Controls the application window (minimize, maximize, close, drag).
/// Implemented by PhotinoWindowController in the Host project.
/// </summary>
public interface IWindowController
{
    /// <summary>Minimizes the window.</summary>
    void Minimize();

    /// <summary>Toggles between maximized and restored state.</summary>
    void ToggleMaximize();

    /// <summary>Closes the window and shuts down the application.</summary>
    void Close();

    /// <summary>Whether the window is currently maximized.</summary>
    bool IsMaximized { get; }

    /// <summary>
    /// Starts a window drag operation. Stores the mouse screen position
    /// and the current window position as reference for subsequent MoveBy calls.
    /// </summary>
    void DragStart(int screenX, int screenY);

    /// <summary>
    /// Moves the window based on the current mouse screen position
    /// relative to where DragStart was called.
    /// </summary>
    void DragMove(int screenX, int screenY);
}
