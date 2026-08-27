using FacultyManagement.Business.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FacultyManagement.Business;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<AccountService>();
        services.AddScoped<AcademicService>();
        services.AddScoped<DivisionService>();
        services.AddScoped<RoomService>();
        services.AddScoped<ScheduleService>();
        services.AddScoped<ScheduleQueryService>();
        services.AddScoped<MarkService>();
        services.AddScoped<MarkImportParser>();
        services.AddScoped<AppealService>();
        services.AddScoped<PromotionService>();
        services.AddScoped<CommunicationService>();
        services.AddScoped<TimetableGeneratorService>();
        services.AddScoped<CatalogQueryService>();
        return services;
    }
}
