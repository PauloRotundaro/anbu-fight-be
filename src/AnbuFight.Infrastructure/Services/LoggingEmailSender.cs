using Microsoft.Extensions.Logging;

namespace AnbuFight.Infrastructure.Services;

/// <summary>
/// Implementação de desenvolvimento: registra a mensagem no log em vez de enviá-la. Permite testar
/// o fluxo de "esqueci minha senha" de ponta a ponta sem provedor de e-mail — basta copiar o link
/// do console. Para produção, registre outra implementação de <see cref="IEmailSender"/>.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "E-mail NÃO enviado (nenhum provedor configurado). Para: {To} | Assunto: {Subject}\n{Body}",
            message.To,
            message.Subject,
            message.Body);

        return Task.CompletedTask;
    }
}
