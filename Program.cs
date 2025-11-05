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
            message = "The URL entered is invalid."
        });
    }

    var existingURL = await context.ShortenedUrls
            .FirstOrDefaultAsync(u => u.Url == request.Url);

    if (existingURL is not null)
    {
        return Results.Conflict(new UrlResponse()
        {
            Id = existingURL.Id,
            Url = existingURL.Url,
            ShortCode = existingURL.ShortCode!,
            CreatedAt = existingURL.CreatedAt,
            UpdatedAt = existingURL.UpdatedAt
        });
    }        

    var newURL = new URLShorted()
    {
        Url = request.Url,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now,
    };
    await context.ShortenedUrls.AddAsync(newURL);
    await context.SaveChangesAsync();

    newURL.ShortCode = Base62.Encode(newURL.Id);

    await context.SaveChangesAsync();

    return Results.Created("/shorten", new UrlResponse()
    {
        Id = newURL.Id,
        ShortCode = newURL.ShortCode,
        Url = newURL.Url,
        CreatedAt = newURL.CreatedAt,
        UpdatedAt = newURL.UpdatedAt
    });

}).WithName("Shorten");

app.MapGet("/shorten/{shortCode}", async (string shortCode, UrlShortenerContext context) =>
{
    var id = Base62.Decode(shortCode);
    var existingURL = await context.ShortenedUrls.FindAsync(id);

    if (existingURL is null)
    {
        return Results.NotFound(new
        {
            message = "The alias code doesn't retrieve any URL."
        });
    }

    var accessCount = existingURL.Clicks ?? 0;
    existingURL.Clicks = accessCount + 1;
    await context.SaveChangesAsync();

    return Results.Ok(new UrlResponse()
    {
        Id = existingURL.Id,
        ShortCode = existingURL.ShortCode!,
        Url = existingURL.Url,
        CreatedAt = existingURL.CreatedAt,
        UpdatedAt = existingURL.UpdatedAt
    });

})
.WithName("GetShorten");

app.MapPut("/shorten/{shortCode}", async (string shortCode, [FromBody] UrlRequest request,
    UrlShortenerContext context) =>
{
    if (!ValidateURL(request.Url))
    {
        return Results.BadRequest(new
        {
            message = "The URL entered is invalid."
        });
    }

    var id = Base62.Decode(shortCode);
    var existingURL = await context.ShortenedUrls.FindAsync(id);

    if (existingURL is null)
    {
        return Results.NotFound(new
        {
            message = "The URL for editing doesn't exist."
        });
    }

    existingURL.Url = request.Url;
    existingURL.UpdatedAt = DateTime.Now;
    await context.SaveChangesAsync();

    return Results.Ok(new UrlResponse()
    {
        Id = existingURL.Id,
        ShortCode = existingURL.ShortCode!,
        Url = existingURL.Url,
        CreatedAt = existingURL.CreatedAt,
        UpdatedAt = existingURL.UpdatedAt
    });

}).WithName("PutShorten");

app.MapDelete("/shorten/{shortCode}", async (string shortCode, UrlShortenerContext context) =>
{
    var id = Base62.Decode(shortCode);
    var existingURL = await context.ShortenedUrls.FindAsync(id);

    if (existingURL is null)
    {
        return Results.NotFound(new
        {
            message = "The URL for delete doesn't exist."
        });
    }

    context.ShortenedUrls.Remove(existingURL);

    await context.SaveChangesAsync();

    return Results.NoContent();

}).WithName("DeleteShorten");

app.MapGet("/shorten/{shortCode}/stats", async (string shortCode, UrlShortenerContext context) =>
{
    var id = Base62.Decode(shortCode);
    var existingURL = await context.ShortenedUrls
        .FindAsync(id);

    if (existingURL is null)
    {
        return Results.NotFound(new
        {
            message = "The URL (for show stats) doesn't exist."
        });
    }

    return Results.Ok(new UrlStatsResponse()
    {
        Id = existingURL.Id,
        ShortCode = existingURL.ShortCode!,
        Url = existingURL.Url,
        CreatedAt = existingURL.CreatedAt,
        UpdatedAt = existingURL.UpdatedAt,
        AccessCount = existingURL.Clicks
    });

}).WithName("GetShortenStats");

app.Run();



bool ValidateURL(string url)
{
    return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri);
}

record UrlRequest(string Url);

class UrlResponse()
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

class UrlStatsResponse : UrlResponse
{
    public int? AccessCount { get; set; }
}