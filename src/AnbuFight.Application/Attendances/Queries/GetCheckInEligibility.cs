using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.Attendances.Queries;

/// <summary>
/// Estado de elegibilidade do aluno, para a tela mostrar o aviso de débito e o motivo do bloqueio
/// antes de o aluno tentar bater ponto.
/// </summary>
public sealed record GetCheckInEligibilityQuery(Guid? StudentId = null) : IRequest<CheckInEligibilityDto>;

public sealed class GetCheckInEligibilityQueryHandler(
    ICurrentUser currentUser,
    CheckInPolicy checkInPolicy)
    : IRequestHandler<GetCheckInEligibilityQuery, CheckInEligibilityDto>
{
    public Task<CheckInEligibilityDto> Handle(
        GetCheckInEligibilityQuery request,
        CancellationToken cancellationToken)
    {
        var studentId = AccessGuard.RestrictStudentFilter(currentUser, request.StudentId)
            ?? throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(GetCheckInEligibilityQuery.StudentId),
                    "Informe o aluno: esta conta não está vinculada a um cadastro de aluno.")
            ]);

        return checkInPolicy.EvaluateAsync(studentId, cancellationToken);
    }
}
