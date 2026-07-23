using Consumer.Models;
using Microsoft.Extensions.Logging;
using Producer;
using Shared;
using Shared.Events;

namespace Consumer.Handlers
{
    public class LeadCreatedHandler
    {
        private readonly ILogger<LeadCreatedHandler> _logger;

        public LeadCreatedHandler(ILogger<LeadCreatedHandler> logger)
        {
            _logger = logger;
        }

        // This method will be called when a LeadCreatedEvent is received
        public async Task Handle(LeadCreatedEvent message,ConsumerDbContext db) // Explicitly requests method injection for the data context scope
        {
            _logger.LogInformation("Background transaction execution started for Lead: {LeadId}", message.LeadId);

            // 1. Check if the tracked lead already exists in our consumer schema space
            var localLead = await db.Leads.FindAsync(message.LeadId);

            if (localLead == null)
            {
                // 2. Add the tracking record to the consumer's isolated database tables
                db.Leads.Add(new trgtLead
                {
                    LeadId = message.LeadId,
                    CreatedAt = message.CreatedAt
                });

                _logger.LogInformation("Successfully inserted new tracked Lead {LeadId} to consumer table.", message.LeadId);
            }
            else
            {
                // 3. Update execution tracking logic if record exists
                localLead.CreatedAt = DateTime.UtcNow.ToString("o");
            }

            // FLUSH CHANGES TO THE WOLVERINE TRANSACTION:
            // This pushes the INSERT/UPDATE SQL commands to the active connection
            // before Wolverine commits the final unit of work.
            await db.SaveChangesAsync();
        }
    }
}

