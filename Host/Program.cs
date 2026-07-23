using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Producer;
using Shared;
using Shared.Events;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "InsureLiv API",
        Description = "An ASP.NET Core Web API for InsureLiv operations."
    });
});


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string is missing.");

builder.UseWolverine(opts =>
{
    // Identifies this monolithic application cluster.
    // When you eventually extract microservices, each separated API will get its own name.
    opts.ServiceName = "InsureLivSystem";


    // --- NEW CODE: TELL WOLVERINE WHERE TO FIND HANDLERS ---
    // You must register any external assemblies that contain Wolverine Handlers.
    // Use any known type from those assemblies (like your DbContext or the Handler itself)
    opts.Discovery.IncludeAssembly(typeof(ProducerDbContext).Assembly);
    opts.Discovery.IncludeAssembly(typeof(ConsumerDbContext).Assembly);
    // opts.Discovery.IncludeAssembly(typeof(LeadCreatedHandler).Assembly); // Also works
    // --------------------------------------------------------

    // 1. Unified Storage Setup (Pattern 2: Shared Schema)
    // All 6 modules will share these envelope tables for Outbox/Inbox tracking.
    opts.UseSqlServerPersistenceAndTransport(
        connectionString,
        schema: "wolverine"
    ).AutoProvision(); // Architect Note: Swap to Weasel schema migrations in Production.

    // 2. Global EF Core Outbox/Inbox Interception
    // Applying this ONCE here enforces the durable outbox/inbox policies 
    // across ALL DbContexts registered in this host.
    opts.UseEntityFrameworkCoreTransactions();

    // 3. Routing (Publishers)
    // Any module in this host can publish this event. 
    // Wolverine will intercept the local module's DbContext transaction.
    opts.PublishMessage<LeadCreatedEvent>().ToSqlServerQueue("lead-processing-queue");

    // Future Kafka Swap: 
    // opts.PublishMessage<LeadCreatedEvent>().ToKafkaTopic("leads.created");

    // 4. Listeners (Consumers)
    // Any module in this host that implements a handler for LeadCreatedEvent 
    // will pick up messages from this queue.
    opts.ListenToSqlServerQueue("lead-processing-queue")
        .UseDurableInbox(); // MANDATORY: Enforces exactly-once processing


    // 5. Forces Wolverine to split multiple handlers for the same event 
    // into independent execution pipelines and message subscriptions.
    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

    // Future Kafka Swap: 
    // opts.ListenToKafkaTopic("leads.created").UseDurableInbox();
});


// -------------------------------------------------------------
// Module-Specific DbContext Registrations
// -------------------------------------------------------------

// Module 1: Source (e.g., Lead generation module, schema: 'src')
builder.Services.AddDbContextWithWolverineIntegration<ProducerDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly("02_Producer")
    ));

// Module 2: Target (e.g., Lead processing module, schema: 'trgt')
builder.Services.AddDbContextWithWolverineIntegration<ConsumerDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly("03_Consumer")
    ));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
