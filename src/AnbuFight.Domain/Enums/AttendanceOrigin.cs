namespace AnbuFight.Domain.Enums;

/// <summary>Quem registrou a presença.</summary>
public enum AttendanceOrigin
{
    /// <summary>Check-in feito pelo próprio aluno.</summary>
    Student = 1,

    /// <summary>Lançamento manual feito pela gestão.</summary>
    Teacher = 2
}
