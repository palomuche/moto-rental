using MotoRentalApi.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiConfig()
       .AddSwaggerConfig()
       .AddDbContextConfig()
       .AddIdentityConfig();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
