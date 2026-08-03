using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.Students.Commands;

/// <summary>
/// O aluno atualiza os próprios dados de contato. Fora do alcance dele, de propósito: e-mail
/// (é a identidade de acesso), data de nascimento, situação e perfil.
/// </summary>
public sealed record UpdateMyProfileCommand(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? EmergencyContact,
    string? EmergencyPhoneNumber) : IRequest;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(command => command.EmergencyContact).MaximumLength(200);
        RuleFor(command => command.EmergencyPhoneNumber).MaximumLength(20);
    }
}

public sealed class UpdateMyProfileCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<UpdateMyProfileCommand>
{
    public async Task Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId
            ?? throw new ForbiddenAccessException("Esta conta não está vinculada a um cadastro de aluno.");

        var student = await context.Students
            .FirstOrDefaultAsync(entity => entity.Id == studentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), studentId);

        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();
        student.PhoneNumber = request.PhoneNumber.Trim();
        student.EmergencyContact = request.EmergencyContact?.Trim();
        student.EmergencyPhoneNumber = request.EmergencyPhoneNumber?.Trim();

        await context.SaveChangesAsync(cancellationToken);
    }
}
