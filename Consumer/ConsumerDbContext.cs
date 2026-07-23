using Consumer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.EntityFrameworkCore;

namespace Producer
{
    public class ConsumerDbContext: DbContext
    {
        public DbSet<trgtLead> Leads { get; set; }

        public ConsumerDbContext(DbContextOptions<ConsumerDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("trgt");

            modelBuilder.Entity<trgtLead>().ToTable("TargetLeads");
            modelBuilder.Entity<trgtLead>().HasKey(l => l.LeadId);
        }
    }
}
