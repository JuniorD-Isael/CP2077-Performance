using System.Runtime.InteropServices;

namespace CP2077Performance.Ui;

/// <summary>
/// A tecla global que alterna o HUD dentro do jogo.
/// </summary>
/// <remarks>
/// O CET instalado nao expoe registro de tecla para os mods, entao a tecla e
/// capturada aqui, no aplicativo, e a intencao chega ao mod por um arquivo no
/// diretorio dele. Por isso o aplicativo precisa estar aberto — em segundo
/// plano, na bandeja — enquanto o jogo roda.
/// </remarks>
internal sealed class GlobalHotkey : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private const int HotkeyId = 0xB007;

    private readonly IntPtr _window;
    private readonly Action _onPressed;
    private bool _registered;

    public GlobalHotkey(IntPtr window, Action onPressed)
    {
        _window = window;
        _onPressed = onPressed;
    }

    /// <summary>Verdadeiro quando a tecla foi reservada.</summary>
    public bool IsRegistered => _registered;

    /// <summary>Registra a tecla. Devolve falso quando ela ja esta em uso.</summary>
    public bool Register(int virtualKey, uint modifiers = 0)
    {
        if (_registered)
        {
            return true;
        }

        _registered = Native.RegisterHotKey(_window, HotkeyId, modifiers | ModNoRepeat, (uint)virtualKey);
        return _registered;
    }

    /// <summary>Devolve a tecla ao sistema.</summary>
    public void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        Native.UnregisterHotKey(_window, HotkeyId);
        _registered = false;
    }

    /// <summary>Chamada pela janela quando a tecla e pressionada.</summary>
    public void HandleMessage(IntPtr message, IntPtr wParam)
    {
        if (message.ToInt32() != WmHotkey || wParam.ToInt32() != (int)HotkeyId)
        {
            return;
        }

        _onPressed();
    }

    public void Dispose() => Unregister();

    private static class Native
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
