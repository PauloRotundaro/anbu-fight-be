namespace AnbuFight.Application.Students.Commands;

public sealed record UpdateStudentCommand(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    StudentStatus Status,
    string? EmergencyContact,
    string? EmergencyPhoneNumber,
    UserRole Role) : IRequest;

public sealed class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator(IGymClock clock)
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Birthdate).PlausibleBirthdate(clock);
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(command => command.EmergencyContact).MaximumLength(200);
        RuleFor(command => command.EmergencyPhoneNumber).MaximumLength(20);
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.Role).IsInEnum();
    }
}

public sealed class UpdateStudentCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateStudentCommand>
{
    public async Task Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .Include(entity => entity.User)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.Id);

        var email = request.Email.Trim().ToLowerInvariant();

        if (email != student.Email &&
            await context.Students.AnyAsync(
                other => other.Email == email && other.Id != student.Id, cancellationToken))
        {
            throw new ConflictException($"A student with the e-mail \"{email}\" already exists.");
        }

        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();
        student.Birthdate = request.Birthdate;
        student.Email = email;
        student.PhoneNumber = request.PhoneNumber.Trim();
        student.Status = request.Status;
        student.EmergencyContact = request.EmergencyContact?.Trim();
        student.EmergencyPhoneNumber = request.EmergencyPhoneNumber?.Trim();
        student.Role = request.Role;

        // The e-mail is the login, so the credential follows the student record.
        if (student.User is not null)
        {
            if (email != student.User.Email &&
                await context.Users.AnyAsync(
                    other => other.Email == email && other.Id != student.User.Id, cancellationToken))
            {
                throw new ConflictException($"A user with the e-mail \"{email}\" already exists.");
            }

            student.User.Email = email;
            student.User.Role = request.Role;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
