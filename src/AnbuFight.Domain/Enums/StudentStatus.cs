namespace AnbuFight.Domain.Enums;

/// <summary>
/// Situação do aluno no sistema. Substitui o antigo booleano <c>IsActive</c> porque
/// "cadastrou-se e aguarda aprovação" e "parou de treinar" são estados diferentes.
/// </summary>
public enum StudentStatus
{
    /// <summary>Auto-cadastro ainda não aprovado pela gestão. Não pode fazer check-in.</summary>
    PendingApproval = 1,

    /// <summary>Aprovado e frequentando.</summary>
    Active = 2,

    /// <summary>Parou de treinar. Mantém todo o histórico.</summary>
    Inactive = 3
}
