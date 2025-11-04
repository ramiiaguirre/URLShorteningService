using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shorten;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<UrlShortenerContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/shorten", async ([FromBody] UrlRequest request, UrlShortenerContext context)  =>
{
    if (!ValidateURL(request.Url))
    {
        return Results.BadRequest(new
        {
            message = "La URL ingresada es inválida"
        });
    }

    var existingURL = await context.ShortenedUrls
            .FirstOrDefaultAsync(u => u.Url == request.Url);

    if (existingURL is not null)
    {
        return Results.Conflict(new UrlResponse()
        {
            Url = existingURL.Url,
            ShortCode = existingURL.ShortCode!,
            CreatedAt = existingURL.CreatedAt,
            UpdatedAt = existingURL.UpdatedAt
        });
    }        

    //Create a resourse
    var newURL = new URLShorted()
    {
        Url = request.Url,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now,
    };
    await context.ShortenedUrls.AddAsync(newURL);
    await context.SaveChangesAsync();

    newURL.ShortCode = Base62.Encode(newURL.Id);

    Console.WriteLine(newURL.ShortCode);
    await context.SaveChangesAsync();

    //Crear urlShort y guardarla.
    return Results.Created("/shorten", new UrlResponse()
    {
        Id = newURL.Id,
        ShortCode = newURL.ShortCode,
        Url = newURL.Url
    });

}).WithName("Shorten");

app.MapGet("/shorten/{shortCode}", async (string shortCode, UrlShortenerContext context) =>
{
    var existingURL = await context.ShortenedUrls
        .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

    if (existingURL is null)
    {
        return Results.NotFound(new
        {
            message = "El código no redirige a ninguna URL"
        });
    }

    return Results.Ok(new UrlResponse()
    {
        Id = existingURL.Id,
        ShortCode = existingURL.ShortCode!,
        Url = existingURL.Url
    });

})
.WithName("GetShorten");

app.MapPut("/shorten/{shortCode}", async (string shortCode, [FromBody] UrlRequest request, UrlShortenerContext context) =>
{
    if (!ValidateURL(request.Url))
    {
        return Results.BadRequest(new
        {
            message = "La URL ingresada es inválida"
        });
    }

    var existingURL = await context.ShortenedUrls
        .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

    if (existingURL is null)
    {
        return Results.NotFound(new
        {
            message = "La URL a editar no existe."
        });
    }

    existingURL.Url = request.Url;
    await context.SaveChangesAsync();

    return Results.Ok(new UrlResponse()
    {
        Id = existingURL.Id,
        ShortCode = existingURL.ShortCode!,
        Url = existingURL.Url
    });

}).WithName("PutShorten");

app.Run();



bool ValidateURL(string url)
{
    return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri);
}

record UrlRequest(string Url);

record UrlResponse()
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
