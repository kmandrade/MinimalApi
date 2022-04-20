using Microsoft.EntityFrameworkCore;
using MinimalWebApi;
using MinimalWebApi.Models;
using MiniValidation;

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

app.MapGet("/fornecedor/{id}", async (
    int id,
    MinimalContext context) =>

    await context.Fornecedores.FindAsync(id)
         //se o find retornar um forncedor
        is Fornecedor fornecedor //se esse fornecedor existir (is) retorna ok
        ? Results.Ok(fornecedor)// se for false retorna notfound
        : Results.NotFound())
    .Produces<Fornecedor>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("BuscaFornecedoresPorId")
    .WithTags("Fornecedor");

app.MapPost("/fornecedor", async (
    MinimalContext context,
    Fornecedor fornecedor) =>
{
    //Install-Package MiniValidation -Version 0.4.2-pre.20220306.48
    //foi instalado esse pacote para utilização de uma validação mais pratica

    if(!MiniValidator.TryValidate(fornecedor,out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Add(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
    ? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor)
    : Results.BadRequest("Error ao cadastrar um fornecedor");

}).ProducesValidationProblem()//metadado que vai informar sobre error que foi tratado documentação mais rica
    .Produces<Fornecedor>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("CriaFornecedor")
    .WithTags("Fornecedor"); 

app.MapPut("/fornecedor/{id}", async (
    int id,
    MinimalContext context,
    Fornecedor fornecedor) =>
{
                                                        //asnotracking para nao ficar com o dado na memoria
    var fornecedorSelecionado = await context.Fornecedores.AsNoTracking<Fornecedor>().FirstOrDefaultAsync(f=>f.Id==id);
    if (fornecedorSelecionado == null) return Results.NotFound();

    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Update(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
        ? Results.NoContent()
        : Results.BadRequest("Error ao salvar o registro");

}).ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("AlteraFornecedor")
    .WithTags("Fornecedor");


app.MapDelete("/fornecedor/{id}", async (
    int id,
    MinimalContext context) =>
{
    var fornecedorSelecionado = await context.Fornecedores.FindAsync(id);
    if (fornecedorSelecionado == null) return Results.NotFound();

    if (!MiniValidator.TryValidate(fornecedorSelecionado, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Remove(fornecedorSelecionado);
    var result = await context.SaveChangesAsync();

    return result > 0
        ? Results.NoContent()
        : Results.BadRequest("Error ao excluir o Fornecedor");

}).Produces(StatusCodes.Status400BadRequest)
    .Produces<Fornecedor>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("RemoveFornecedor")
    .WithTags("Fornecedor");



app.Run();

