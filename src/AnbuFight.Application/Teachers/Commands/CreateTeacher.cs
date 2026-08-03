namespace AnbuFight.Application.Teachers.Commands;

/// <summary>
/// Registers a teacher and, when a password is supplied, the credential that lets them sign in.
/// </summary>
/// <param name="Role">Perfil de acesso da credencial criada. Padrão: <c>Teacher</c>.</param>
/// <param name="Password">Opcional. Informe para dar acesso ao portal; mínimo de 8 caracteres.</param>
public sealed record CreateTeacherCommand(
    string FirstName,
    string LastName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    UserRole Role = UserRole.Teacher,
    string? Password = null) : IRequest<Guid>;

public sealed class CreateTeacherCommandValidator : AbstractValidator<CreateTeacherCommand>
{
    public CreateTeacherCommandValidator(IGymClock clock)
    {
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Birthdate).PlausibleBirthdate(clock);
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Role).IsInEnum();
        RuleFor(command => command.Password).OptionalPassword();
    }
}

public sealed class CreateTeacherCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    : IRequestHandler<CreateTeacherCommand, Guid>
{
    public async Task<Guid> Handle(CreateTeacherCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await context.Teachers.AnyAsync(teacher => teacher.Email == email, cancellationToken))
        {
            throw new ConflictException($"A teacher with the e-mail \"{email}\" already exists.");
        }

        var teacher = new Teacher
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Birthdate = request.Birthdate,
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            Role = request.Role
        };

        context.Teachers.Add(teacher);

        if (!string.IsNullOrEmpty(request.Password))
        {
            if (await context.Users.AnyAsync(user => user.Email == email, cancellationToken))
            {
                throw new ConflictException($"A user with the e-mail \"{email}\" already exists.");
            }

            context.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(request.Password),
                Role = request.Role,
                TeacherId = teacher.Id
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return teacher.Id;
    }
}
