using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Models
{
    public class trgtLead
    {
        public Guid LeadId { get; set; }
        public string CreatedAt { get; set; } = DateTime.Now.ToString("o");
    }
}
