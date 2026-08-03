namespace AnbuFight.Application.Auth.Commands;

/// <summary>
/// Auto-cadastro público. Cria o aluno e a credencial numa transação só, sempre com perfil
/// <c>Student</c> e situação <c>PendingApproval</c> — nenhum campo do corpo influencia isso,
/// senão qualquer pessoa se cadastraria como administrador.
/// </summary>
public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    string Password) : IRequest<Guid>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(IGymClock clock)
    {
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Birthdate).PlausibleBirthdate(clock);
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Password).RequiredPassword();
    }
}

public sealed class RegisterCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterCommand, Guid>
{
    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken =
            await context.Students.AnyAsync(student => student.Email == email, cancellationToken) ||
            await context.Users.AnyAsync(user => user.Email == email, cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException(
                $"Já existe um cadastro com o e-mail \"{email}\". Tente entrar ou recuperar a senha.");
        }

        var student = new Student
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Birthdate = request.Birthdate,
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            Status = StudentStatus.PendingApproval,
            Role = UserRole.Student
        };

        context.Students.Add(student);

        context.Users.Add(new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.Student,
            StudentId = student.Id
        });

        await context.SaveChangesAsync(cancellationToken);

        return student.Id;
    }
}
