using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MinimalWebApi;
using MinimalWebApi.Models;
using MiniValidation;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API Sample",
        Description = "Developed by Eduardo Pires - Owner @ desenvolvedor.io",
        Contact = new OpenApiContact { Name = "Eduardo Pires", Email = "contato@eduardopires.net.br" },
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });

});



builder.Services.AddDbContext<MinimalContext>(opt => 
opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("MinimalWebApi")));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "appsettings");
builder.Services.AddMvc();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();
app.UseHttpsRedirection();

//O model precisa ser passado por ultimo
app.MapPost("/registroUsuario",[AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser registerUser
         ) =>
{
    if (registerUser == null)
        return Results.BadRequest("Usuário não informado");

    if (!MiniValidator.TryValidate(registerUser, out var errors))
        return Results.ValidationProblem(errors);

    //objeto que ira ser persistido no idendity
    var user = new IdentityUser
    {
        UserName = registerUser.Email,
        Email = registerUser.Email,
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(user, registerUser.Password);

    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    var jwt = new JwtBuilder()
                .WithUserManager(userManager)
                .WithJwtSettings(appJwtSettings.Value)
                .WithEmail(user.Email)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .BuildUserResponse();

    return Results.Ok(jwt);

}).ProducesValidationProblem()
      .Produces(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithName("RegistroUsuario")
      .WithTags("Usuario");


app.MapPost("/login", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        LoginUser loginUser) =>
{
    if (loginUser == null)
        return Results.BadRequest("Usuário não informado");

    if (!MiniValidator.TryValidate(loginUser, out var errors))
        return Results.ValidationProblem(errors);

    var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

    if (result.IsLockedOut)
        return Results.BadRequest("Usuário bloqueado");

    if (!result.Succeeded)
        return Results.BadRequest("Usuário ou senha inválidos");

    var jwt = new JwtBuilder()
                .WithUserManager(userManager)
                .WithJwtSettings(appJwtSettings.Value)
                .WithEmail(loginUser.Email)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .BuildUserResponse();

    return Results.Ok(jwt);

}).ProducesValidationProblem()
      .Produces(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithName("LoginUsuario")
      .WithTags("Usuario");



app.MapGet("/fornecedor", [AllowAnonymous] async
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

app.MapPost("/fornecedor",[Authorize] async (
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

app.MapPut("/fornecedor/{id}", [Authorize] async (
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


app.MapDelete("/fornecedor/{id}", [Authorize] async (
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

