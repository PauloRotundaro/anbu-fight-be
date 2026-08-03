using AnbuFight.Api.Endpoints;

namespace AnbuFight.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapStudentEndpoints();
        app.MapTeacherEndpoints();
        app.MapPlanEndpoints();
        app.MapStudentPlanEndpoints();
        app.MapPaymentEndpoints();
        app.MapClassEndpoints();
        app.MapClassSessionEndpoints();
        app.MapAttendanceEndpoints();
        app.MapReportEndpoints();

        return app;
    }
}
