using Photino.NET;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Host;

/// <summary>
/// Bridges IWindowController to the Photino window instance.
/// Window reference is set after creation in PhotinoHosting.Start().
/// </summary>
public class PhotinoWindowController : IWindowController
{
    private PhotinoWindow? _window;

    // Drag state: mouse start position and window start position
    private int _dragStartMouseX;
    private int _dragStartMouseY;
    private int _dragStartWindowX;
    private int _dragStartWindowY;

    public bool IsMaximized => _window?.Maximized ?? false;

    public void SetWindow(PhotinoWindow window)
    {
        _window = window;
    }

    public void Minimize()
    {
        _window?.SetMinimized(true);
    }

    public void ToggleMaximize()
    {
        if (_window is null)
        {
            return;
        }

        _window.SetMaximized(!_window.Maximized);
    }

    public void Close()
    {
        _window?.Close();
    }

    public void DragStart(int screenX, int screenY)
    {
        if (_window is null)
        {
            return;
        }

        _dragStartMouseX = screenX;
        _dragStartMouseY = screenY;
        _dragStartWindowX = _window.Left;
        _dragStartWindowY = _window.Top;
    }

    public void DragMove(int screenX, int screenY)
    {
        if (_window is null)
        {
            return;
        }

        int deltaX = screenX - _dragStartMouseX;
        int deltaY = screenY - _dragStartMouseY;

        _window.SetLeft(_dragStartWindowX + deltaX);
        _window.SetTop(_dragStartWindowY + deltaY);
    }
}
