using GrammarService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN DE SERVICIOS ====================

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

// ✅ Habilitar CORS (debe ir antes de builder.Build())
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy => policy
            .WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});


// ==================== CONSTRUCCIÓN DE LA APLICACIÓN ====================

var app = builder.Build();

// ==================== CONFIGURACIÓN DEL PIPELINE ====================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Usar CORS antes de los controladores
app.UseCors("AllowReact");

// Forzar HTTPS
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// ==================== CREAR BD SI NO EXISTE ====================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GrammarContext>();
    db.Database.EnsureCreated();
}

app.Run();
