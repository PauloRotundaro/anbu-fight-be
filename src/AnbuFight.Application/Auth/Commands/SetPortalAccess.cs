namespace AnbuFight.Application.Auth.Commands;

/// <summary>
/// A gestão concede acesso ao portal para quem ainda não tem, ou redefine a senha de quem
/// esqueceu. É a rede de segurança quando não há e-mail configurado.
/// </summary>
public sealed record SetStudentPortalAccessCommand(Guid StudentId, string Password) : IRequest;

public sealed class SetStudentPortalAccessCommandValidator
    : AbstractValidator<SetStudentPortalAccessCommand>
{
    public SetStudentPortalAccessCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.Password).RequiredPassword();
    }
}

public sealed class SetStudentPortalAccessCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IGymClock clock)
    : IRequestHandler<SetStudentPortalAccessCommand>
{
    public async Task Handle(SetStudentPortalAccessCommand request, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .Include(entity => entity.User)
                .ThenInclude(user => user!.RefreshTokens)
            .FirstOrDefaultAsync(entity => entity.Id == request.StudentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.StudentId);

        var credential = await PortalAccess.GrantAsync(
            context,
            student.User,
            student.Email,
            student.Role,
            passwordHasher.Hash(request.Password),
            clock.Now,
            cancellationToken);

        credential.StudentId = student.Id;

        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record SetTeacherPortalAccessCommand(Guid TeacherId, string Password) : IRequest;

public sealed class SetTeacherPortalAccessCommandValidator
    : AbstractValidator<SetTeacherPortalAccessCommand>
{
    public SetTeacherPortalAccessCommandValidator()
    {
        RuleFor(command => command.TeacherId).NotEmpty();
        RuleFor(command => command.Password).RequiredPassword();
    }
}

public sealed class SetTeacherPortalAccessCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IGymClock clock)
    : IRequestHandler<SetTeacherPortalAccessCommand>
{
    public async Task Handle(SetTeacherPortalAccessCommand request, CancellationToken cancellationToken)
    {
        var teacher = await context.Teachers
            .Include(entity => entity.User)
                .ThenInclude(user => user!.RefreshTokens)
            .FirstOrDefaultAsync(entity => entity.Id == request.TeacherId, cancellationToken)
            ?? throw new NotFoundException(nameof(Teacher), request.TeacherId);

        var credential = await PortalAccess.GrantAsync(
            context,
            teacher.User,
            teacher.Email,
            teacher.Role,
            passwordHasher.Hash(request.Password),
            clock.Now,
            cancellationToken);

        credential.TeacherId = teacher.Id;

        await context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Criação ou redefinição de credencial, comum a alunos e professores.</summary>
internal static class PortalAccess
{
    /// <summary>
    /// Devolve a credencial já pronta para ser vinculada ao aluno ou ao professor.
    /// Redefinir a senha de uma credencial existente derruba as sessões abertas.
    /// </summary>
    public static async Task<User> GrantAsync(
        IApplicationDbContext context,
        User? existingUser,
        string email,
        UserRole role,
        string passwordHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (existingUser is not null)
        {
            existingUser.PasswordHash = passwordHash;
            existingUser.IsActive = true;
            existingUser.RevokeSessions(now);

            return existingUser;
        }

        if (await context.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new ConflictException($"Já existe um usuário com o e-mail \"{email}\".");
        }

        var created = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            Role = role
        };

        context.Users.Add(created);

        return created;
    }
}
