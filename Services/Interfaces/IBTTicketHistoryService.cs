using BTAnshDesai.Models;

namespace BTAnshDesai.Services.Interfaces
{
    public interface IBTTicketHistoryService
    {
        public Task AddHistoryAsync(Ticket oldTicket, Ticket newTicket, string userId);
        public Task<List<TicketHistory>> GetCompanyTicketsHistoriesAsync(int companyId);
        public Task<List<TicketHistory>> GetProjectTicketsHistoriesAsync(int projectId, int companyId);
    }
}
