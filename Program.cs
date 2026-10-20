using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Middleware;
using UserManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Logging wraps error handling so it records the final response status.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<TokenAuthenticationMiddleware>();

var users = new ConcurrentDictionary<int, UserRecord>();
var nextId = 0;

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));


app.MapGet("/api/users", () => Results.Ok(users.Values.OrderBy(user => user.Id).ToArray()));

app.MapGet("/api/users/{id:int}", (int id) =>
    users.TryGetValue(id, out var user) ? Results.Ok(user) : Results.NotFound(new { error = "User not found." }));

app.MapPost("/api/users", (UserRecord? input) =>
{
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var id = Interlocked.Increment(ref nextId);
    var user = Clean(input!, id);
    users[id] = user;
    return Results.Created($"/api/users/{id}", user);
});

app.MapPut("/api/users/{id:int}", (int id, UserRecord? input) =>
{
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    if (!users.ContainsKey(id)) return Results.NotFound(new { error = "User not found." });
    users[id] = Clean(input!, id);
    return Results.Ok(users[id]);
});

app.MapDelete("/api/users/{id:int}", (int id) =>
    users.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound(new { error = "User not found." }));

app.Run();

static UserRecord Clean(UserRecord input, int id) => new()
{
    Id = id,
    Name = (input.Name ?? string.Empty).Trim(),
    Email = (input.Email ?? string.Empty).Trim(),
    Department = (input.Department ?? string.Empty).Trim()
};

static Dictionary<string, string[]> Validate(UserRecord? input)
{
    if (input is null) return new() { ["body"] = ["A JSON user object is required."] };
    var normalizedInput = Clean(input, input.Id);
    var context = new ValidationContext(normalizedInput);
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(normalizedInput, context, results, validateAllProperties: true);
    var errors = results
        .SelectMany(result => (result.MemberNames.Any() ? result.MemberNames : ["body"])
            .Select(member => (member, message: result.ErrorMessage ?? "Invalid value.")))
        .GroupBy(item => item.member)
        .ToDictionary(group => group.Key, group => group.Select(item => item.message).ToArray());
    foreach (var (field, value) in new[] { ("Name", normalizedInput.Name), ("Email", normalizedInput.Email), ("Department", normalizedInput.Department) })
        if (string.IsNullOrWhiteSpace(value)) errors[field] = [field + " cannot be blank."];
    return errors;
}
