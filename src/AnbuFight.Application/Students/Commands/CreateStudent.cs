namespace AnbuFight.Application.Students.Commands;

/// <summary>
/// Registers a student and, when a password is supplied, the credential that lets them sign in.
/// </summary>
/// <param name="Status">Situação inicial. Cadastro feito pela gestão nasce <c>Active</c>.</param>
/// <param name="Role">Perfil de acesso da credencial criada. Padrão: <c>Student</c>.</param>
/// <param name="Password">Opcional. Informe para dar acesso ao portal; mínimo de 8 caracteres.</param>
public sealed record CreateStudentCommand(
    string FirstName,
    string LastName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    StudentStatus Status = StudentStatus.Active,
    string? EmergencyContact = null,
    string? EmergencyPhoneNumber = null,
    UserRole Role = UserRole.Student,
    string? Password = null) : IRequest<Guid>;

public sealed class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator(IGymClock clock)
    {
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Birthdate).PlausibleBirthdate(clock);
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(command => command.EmergencyContact).MaximumLength(200);
        RuleFor(command => command.EmergencyPhoneNumber).MaximumLength(20);
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.Role).IsInEnum();
        RuleFor(command => command.Password).OptionalPassword();
    }
}

public sealed class CreateStudentCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher)
    : IRequestHandler<CreateStudentCommand, Guid>
{
    public async Task<Guid> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await context.Students.AnyAsync(student => student.Email == email, cancellationToken))
        {
            throw new ConflictException($"A student with the e-mail \"{email}\" already exists.");
        }

        var student = new Student
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Birthdate = request.Birthdate,
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            Status = request.Status,
            EmergencyContact = request.EmergencyContact?.Trim(),
            EmergencyPhoneNumber = request.EmergencyPhoneNumber?.Trim(),
            Role = request.Role
        };

        context.Students.Add(student);

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
                StudentId = student.Id
            });
        }

        // Student and user are persisted in the same transaction: no orphan credential can be left behind.
        await context.SaveChangesAsync(cancellationToken);

        return student.Id;
    }
}
