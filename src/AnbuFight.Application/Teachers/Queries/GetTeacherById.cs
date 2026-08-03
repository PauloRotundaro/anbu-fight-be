namespace AnbuFight.Application.Teachers.Queries;

public sealed record GetTeacherByIdQuery(Guid Id) : IRequest<TeacherDto>;

public sealed class GetTeacherByIdQueryValidator : AbstractValidator<GetTeacherByIdQuery>
{
    public GetTeacherByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}

public sealed class GetTeacherByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetTeacherByIdQuery, TeacherDto>
{
    public async Task<TeacherDto> Handle(GetTeacherByIdQuery request, CancellationToken cancellationToken) =>
        await context.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.Id == request.Id)
            .Select(TeacherDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(nameof(Teacher), request.Id);
}
