namespace AnbuFight.Application.Common.Interfaces;

/// <summary>
/// Tempo no fuso da academia. Existe porque "hoje" precisa ser o dia de quem está no tatame:
/// às 21h de São Paulo já é o dia seguinte em UTC, e isso decidiria errado o que está vencido
/// e qual é a aula de hoje.
/// </summary>
public interface IGymClock
{
    /// <summary>Instante atual, absoluto.</summary>
    DateTimeOffset Now { get; }

    /// <summary>Data corrente no fuso da academia.</summary>
    DateOnly Today { get; }

    /// <summary>Converte uma data e um horário locais no instante absoluto correspondente.</summary>
    DateTimeOffset ToInstant(DateOnly date, TimeOnly time);

    /// <summary>Data local correspondente a um instante absoluto.</summary>
    DateOnly ToLocalDate(DateTimeOffset instant);
}
