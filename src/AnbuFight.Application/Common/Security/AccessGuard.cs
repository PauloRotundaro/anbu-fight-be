namespace AnbuFight.Application.Common.Security;

/// <summary>
/// Row-level rules that the authorization policies cannot express: a signed-in student may only
/// reach its own records. Admins and teachers are filtered at the endpoint level.
/// </summary>
internal static class AccessGuard
{
    public static void EnsureCanReachStudentData(ICurrentUser currentUser, Guid studentId)
    {
        if (currentUser.IsStudent && currentUser.StudentId != studentId)
        {
            throw new ForbiddenAccessException("You can only access your own records.");
        }
    }

    public static void EnsureCanReachTeacherData(ICurrentUser currentUser, Guid teacherId)
    {
        if (currentUser.IsTeacher && currentUser.TeacherId != teacherId)
        {
            throw new ForbiddenAccessException("You can only access your own records.");
        }
    }

    /// <summary>
    /// Narrows a list query to the caller's own student when the caller is a student,
    /// so no filter combination can ever leak someone else's data.
    /// </summary>
    public static Guid? RestrictStudentFilter(ICurrentUser currentUser, Guid? requestedStudentId)
    {
        if (!currentUser.IsStudent)
        {
            return requestedStudentId;
        }

        if (requestedStudentId is not null && requestedStudentId != currentUser.StudentId)
        {
            throw new ForbiddenAccessException("You can only access your own records.");
        }

        return currentUser.StudentId ?? Guid.Empty;
    }
}
