using System.Diagnostics;

namespace CP2077Performance.Core.GameInstallation;

/// <summary>
/// Impede que o perfil seja aplicado com o jogo em execucao.
/// </summary>
/// <remarks>
/// O jogo carrega as opcoes de menu no startup, valida cada uma contra o
/// esquema, e descarrega o estado em memoria para o arquivo ao encerrar.
/// Um valor escrito por ferramenta enquanto o jogo roda seria sobrescrito
/// nessa descarga. Por isso a operacao e recusada.
/// </remarks>
public sealed class GameRunningGuard
{
    public const string ProcessName = "Cyberpunk2077";

    public bool IsGameRunning() =>
        Process.GetProcessesByName(ProcessName).Length > 0;

    /// <summary>
    /// Explicacao exibida ao usuario quando o jogo esta em execucao.
    /// </summary>
    public static string RefusalMessage =>
        "O jogo esta em execucao. Ele carrega as opcoes de menu no startup e " +
        "descarrega o estado em memoria para o arquivo ao encerrar, o que " +
        "anularia a aplicacao. Feche o Cyberpunk 2077 e tente de novo.";

    /// <summary>
    /// Verificacao imediatamente antes de escrever. O jogo pode ter sido
    /// fechado entre a checagem inicial e a aplicacao; nesse caso a operacao
    /// deve prosseguir.
    /// </summary>
    /// <param name="outReason">Motivo da recusa, quando houver.</param>
    /// <returns><c>true</c> quando a operacao pode prosseguir.</returns>
    public bool CanProceed(out string? reason)
    {
        if (IsGameRunning())
        {
            reason = RefusalMessage;
            return false;
        }

        reason = null;
        return true;
    }
}
