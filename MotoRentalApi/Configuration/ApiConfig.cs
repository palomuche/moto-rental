using MotoRentalApi.Data;

namespace MotoRentalApi.Configuration
{
    public static class ApiConfig
    {
        public static WebApplicationBuilder AddApiConfig(this WebApplicationBuilder builder)
        {

            builder.Services.AddControllers();
            builder.Services.AddSingleton<LocalStorageService>();

            return builder;
        }
    }
}
