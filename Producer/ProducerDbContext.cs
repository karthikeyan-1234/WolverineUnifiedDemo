using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.EntityFrameworkCore;

namespace Producer
{
    public class ProducerDbContext: DbContext
    {
        public DbSet<srcLead> Leads { get; set; }

        public ProducerDbContext(DbContextOptions<ProducerDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("src");

            modelBuilder.Entity<srcLead>().HasKey(l => l.LeadId);
        }
    }
}
