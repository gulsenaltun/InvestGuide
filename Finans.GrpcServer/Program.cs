using Finans.GrpcServer.Services;

var builder = WebApplication.CreateBuilder(args);

// gRPC servisini ekle
builder.Services.AddGrpc();

var app = builder.Build();

// Servisimizi endpoint olarak map'le
app.MapGrpcService<FiyatService>();
app.MapGet("/", () => "Bu bir gRPC Fiyat Sunucusudur. Lütfen Client ile bağlanın.");

app.Run();