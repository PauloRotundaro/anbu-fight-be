namespace AnbuFight.Domain.Enums;

/// <summary>Por que o aluno pode ou não fazer check-in. Avaliado nesta ordem de precedência.</summary>
public enum CheckInBlockReason
{
    /// <summary>Liberado.</summary>
    Ok = 1,

    /// <summary>Aluno pendente de aprovação ou inativo.</summary>
    InactiveStudent = 2,

    /// <summary>Sem nenhuma matrícula.</summary>
    NoActiveEnrollment = 3,

    /// <summary>Atraso acima da tolerância configurada.</summary>
    OverdueLimitExceeded = 4
}
