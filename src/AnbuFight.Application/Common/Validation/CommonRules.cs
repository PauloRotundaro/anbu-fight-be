namespace AnbuFight.Application.Common.Validation;

/// <summary>
/// Regras repetidas em vários comandos. Centralizá-las garante que o limite e a mensagem
/// sejam os mesmos em todo lugar.
/// </summary>
public static class CommonRules
{
    private const int MaxPlausibleAgeInYears = 120;

    public const int MinimumPasswordLength = 8;

    public const int MaximumPasswordLength = 128;

    private const string PasswordLengthMessage =
        "A senha deve ter entre 8 e 128 caracteres.";

    public static IRuleBuilderOptions<T, DateOnly> PlausibleBirthdate<T>(
        this IRuleBuilder<T, DateOnly> ruleBuilder,
        IGymClock clock)
    {
        var today = clock.Today;

        return ruleBuilder
            .NotEmpty()
            .LessThan(today).WithMessage("A data de nascimento deve estar no passado.")
            .GreaterThan(today.AddYears(-MaxPlausibleAgeInYears))
            .WithMessage("A data de nascimento não é plausível.");
    }

    /// <summary>Senha opcional: o tamanho só é cobrado quando algo foi informado.</summary>
    public static IRuleBuilderOptions<T, string?> OptionalPassword<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(HasValidLength)
            .WithMessage(PasswordLengthMessage);

    /// <summary>Senha obrigatória.</summary>
    public static IRuleBuilderOptions<T, string> RequiredPassword<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .Must(HasValidLength)
            .WithMessage(PasswordLengthMessage);

    private static bool HasValidLength(string? password) =>
        string.IsNullOrEmpty(password) ||
        (password.Length >= MinimumPasswordLength && password.Length <= MaximumPasswordLength);
}
