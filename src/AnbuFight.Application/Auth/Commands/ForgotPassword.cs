using Microsoft.Extensions.Options;

namespace AnbuFight.Application.Auth.Commands;

/// <summary>
/// Dispara o e-mail de redefinição. Responde igual para e-mail existente ou não, para que a rota
/// pública não sirva de sonda de quem tem cadastro.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator() =>
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256);
}

public sealed class ForgotPasswordCommandHandler(
    IApplicationDbContext context,
    ITokenProvider tokenProvider,
    IEmailSender emailSender,
    IOptions<GymOptions> options)
    : IRequestHandler<ForgotPasswordCommand>
{
    private readonly GymOptions _options = options.Value;

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await context.Users
            .FirstOrDefaultAsync(entity => entity.Email == email && entity.IsActive, cancellationToken);

        if (user is null)
        {
            return;
        }

        var token = tokenProvider.CreateOpaqueToken(TimeSpan.FromHours(_options.PasswordResetTokenHours));

        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = token.Hash,
            ExpiresAt = token.ExpiresAt
        });

        await context.SaveChangesAsync(cancellationToken);

        var link = $"{_options.PasswordResetUrl.TrimEnd('/')}?token={Uri.EscapeDataString(token.Value)}";

        await emailSender.SendAsync(
            new EmailMessage(
                user.Email,
                "Redefinição de senha — Anbu Fight",
                $"""
                 Recebemos um pedido para redefinir a sua senha.

                 Use o link abaixo (válido por {_options.PasswordResetTokenHours} horas):
                 {link}

                 Se não foi você, ignore esta mensagem: nada muda até o link ser usado.
                 """),
            cancellationToken);
    }
}
