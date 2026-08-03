namespace AnbuFight.Application.Teachers.Commands;

public sealed record UpdateTeacherCommand(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    UserRole Role) : IRequest;

public sealed class UpdateTeacherCommandValidator : AbstractValidator<UpdateTeacherCommand>
{
    public UpdateTeacherCommandValidator(IGymClock clock)
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Birthdate).PlausibleBirthdate(clock);
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Role).IsInEnum();
    }
}

public sealed class UpdateTeacherCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateTeacherCommand>
{
    public async Task Handle(UpdateTeacherCommand request, CancellationToken cancellationToken)
    {
        var teacher = await context.Teachers
            .Include(entity => entity.User)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Teacher), request.Id);

        var email = request.Email.Trim().ToLowerInvariant();

        if (email != teacher.Email &&
            await context.Teachers.AnyAsync(
                other => other.Email == email && other.Id != teacher.Id, cancellationToken))
        {
            throw new ConflictException($"A teacher with the e-mail \"{email}\" already exists.");
        }

        teacher.FirstName = request.FirstName.Trim();
        teacher.LastName = request.LastName.Trim();
        teacher.Birthdate = request.Birthdate;
        teacher.Email = email;
        teacher.PhoneNumber = request.PhoneNumber.Trim();
        teacher.Role = request.Role;

        if (teacher.User is not null)
        {
            if (email != teacher.User.Email &&
                await context.Users.AnyAsync(
                    other => other.Email == email && other.Id != teacher.User.Id, cancellationToken))
            {
                throw new ConflictException($"A user with the e-mail \"{email}\" already exists.");
            }

            teacher.User.Email = email;
            teacher.User.Role = request.Role;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
