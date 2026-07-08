using Microsoft.Agents.AI.DevUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.AddDevUI();
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map DevUI endpoints 
app.MapDevUI();
app.MapOpenAIResponses();
app.MapOpenAIConversations();

app.Run();
