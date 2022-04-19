using Microsoft.EntityFrameworkCore;
using MinimalWebApi;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MinimalContext>(opt => 
opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/fornecedor", async
    (MinimalContext context) =>
    await context.Fornecedores.ToListAsync())
    .WithName("BuscaFornecedores")
    .WithTags("Fornecedor");



app.Run();

