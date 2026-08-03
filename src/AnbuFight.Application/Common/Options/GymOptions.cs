namespace AnbuFight.Application.Common.Options;

/// <summary>
/// Políticas operacionais da academia. Ficam em configuração porque mudam por decisão de negócio,
/// não por deploy de código — e o frontend lê os valores da API em vez de duplicá-los.
/// </summary>
public sealed class GymOptions
{
    public const string SectionName = "Gym";

    /// <summary>Fuso usado para "hoje", vencimentos e horários de aula.</summary>
    public string TimeZone { get; set; } = "America/Sao_Paulo";

    /// <summary>Quantos minutos antes do início da aula o check-in é liberado.</summary>
    public int CheckInWindowMinutesBefore { get; set; } = 60;

    /// <summary>Dias de atraso tolerados antes de bloquear o check-in.</summary>
    public int OverdueGraceDays { get; set; } = 5;

    /// <summary>Por quantas horas o link de redefinição de senha é válido.</summary>
    public int PasswordResetTokenHours { get; set; } = 2;

    /// <summary>Base do link enviado no e-mail de redefinição de senha.</summary>
    public string PasswordResetUrl { get; set; } = "http://localhost:3000/redefinir-senha";
}
