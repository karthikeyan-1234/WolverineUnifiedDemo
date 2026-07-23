namespace Producer
{    public class srcLead
    {
        public Guid LeadId { get; set; }
        public string CreatedAt { get; set; } = DateTime.Now.ToString("o");
    }
}
