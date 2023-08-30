using System.ComponentModel;

namespace BTAnshDesai.Models
{
    public class TicketStatus
    {
        public int Id { get; set; }

        [DisplayName("Status Name")]
        public string? Name { get; set; }
    }
}
