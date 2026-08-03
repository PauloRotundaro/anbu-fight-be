namespace AnbuFight.Application.Common.Interfaces;

/// <summary>
/// Envio de e-mail transacional. A implementação é trocável: em desenvolvimento apenas registra
/// a mensagem no log, e um provedor real entra depois sem tocar nos casos de uso.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(string To, string Subject, string Body);
