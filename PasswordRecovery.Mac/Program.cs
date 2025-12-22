using SoapCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Servisleri kaydet
builder.Services.AddSingleton<IAuthService, AuthService>();

var app = builder.Build();

// SOAP Endpoint'ini tanımla (Servis burada yayınlanacak)
app.UseRouting();
app.UseEndpoints(endpoints => {
    endpoints.UseSoapEndpoint<IAuthService>("/AuthService.svc", new SoapEncoderOptions(), SoapSerializer.XmlSerializer);
});

app.Run("http://localhost:5000"); // Sabit port verelim ki karışmasın