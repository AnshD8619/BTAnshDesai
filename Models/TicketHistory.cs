using System.ComponentModel;

namespace BTAnshDesai.Models
{
	public class TicketHistory
	{
		public int Id { get; set; }

		[DisplayName("Ticket")]
		public int TicketId { get; set; }

		[DisplayName("Updated Item")]
		public string? Property { get; set; }

		[DisplayName("Previous")]
		public string? OldValue { get; set; }

		[DisplayName("Current")]
		public string? NewValue { get; set; }

		[DisplayName("Date Modified")]
		public DateTimeOffset Created { get; set; }

		[DisplayName("Description of change")]
		public string? Description { get; set; }

		[DisplayName("Team Member")]
		public string? UserId { get; set; }

		public virtual Ticket? Ticket { get; set; }

		public virtual BTUser? User { get; set; }
	}
}
