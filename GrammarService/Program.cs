using GrammarService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configurar base de datos SQLite
builder.Services.AddDbContext<GrammarContext>(options =>
    options.UseSqlite("Data Source=grammar.db"));

// Registrar el servicio de gramática
builder.Services.AddScoped<GrammarService.Services.GrammarService>();

// Configurar rutas en minúsculas
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Agregar controladores
builder.Services.AddControllers();

// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Crear la base de datos si no existe
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GrammarContext>();
    db.Database.EnsureCreated();
}



// Forzar HTTPS
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();