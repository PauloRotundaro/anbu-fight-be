using System.Linq.Expressions;

namespace AnbuFight.Application.StudentPlans;

/// <param name="PlanValue">Valor negociado com este aluno, independente de reajustes no plano.</param>
/// <param name="DueDate">Vencimento da próxima cobrança desta matrícula.</param>
public sealed record StudentPlanDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid PlanId,
    string PlanName,
    PlanType PlanType,
    decimal PlanValue,
    DateOnly DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static Expression<Func<StudentPlan, StudentPlanDto>> Projection { get; } = enrollment =>
        new StudentPlanDto(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.Student.FirstName + " " + enrollment.Student.LastName,
            enrollment.PlanId,
            enrollment.Plan.Name,
            enrollment.Plan.Type,
            enrollment.PlanValue,
            enrollment.DueDate,
            enrollment.CreatedAt,
            enrollment.UpdatedAt);
}
