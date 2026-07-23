using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Producer;
using Shared;
using Shared.Events;
using Wolverine.EntityFrameworkCore;


namespace Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProducerController : ControllerBase
    {
        public ProducerController() { }

        //POST Add new Lead to PublisherDBContext and use Wolverine to publish message to in-memory bus
        [HttpPost("AddLead")]
        public async Task<IActionResult> AddLead([FromBody] srcLead lead, [FromServices] IDbContextOutbox<ProducerDbContext> outbox)
        {

            // 1. Stage the entity update inside the Outbox DbContext instance
            outbox.DbContext.Leads.Add(lead);

            // 2. Stage the event message. 
            // Because this context is enlisted in an outbox transaction, this message is held in-memory 
            // and mapped to your database transaction context. It does NOT hit the wire yet.
            var leadEvent = new LeadCreatedEvent(lead.LeadId, lead.CreatedAt);
            await outbox.PublishAsync(leadEvent);

            // 3. ATOMIC COMPLETION (Combines a, b, and c):
            // This single execution saves the Lead entity to your domain table, 
            // serializes and saves the Wolverine message envelope to your SQL Outbox table,
            // and commits them both in a single, atomic database transaction unit.
            // Once committed, Wolverine instantly flushes the staged message to the bus queue.
            await outbox.SaveChangesAndFlushMessagesAsync();

            return Ok(new { Message = "Lead added and message published." });
        }
    }
}
