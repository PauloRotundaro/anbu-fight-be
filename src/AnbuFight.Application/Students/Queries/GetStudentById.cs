using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.Students.Queries;

public sealed record GetStudentByIdQuery(Guid Id) : IRequest<StudentDto>;

public sealed class GetStudentByIdQueryValidator : AbstractValidator<GetStudentByIdQuery>
{
    public GetStudentByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}

public sealed class GetStudentByIdQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetStudentByIdQuery, StudentDto>
{
    public async Task<StudentDto> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        AccessGuard.EnsureCanReachStudentData(currentUser, request.Id);

        return await context.Students
            .AsNoTracking()
            .Where(student => student.Id == request.Id)
            .Select(StudentDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.Id);
    }
}
