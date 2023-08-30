
using BTAnshDesai.Controllers.Interfaces;
using BTAnshDesai.Data;
using BTAnshDesai.Models;
using Microsoft.EntityFrameworkCore;

namespace BTAnshDesai.Services
{
    public class BTInviteService : IBTInviteService
    {
        private readonly ApplicationDbContext _context;

        public BTInviteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AcceptInviteAsync(Guid? token, string userId, int companyId)
        {
            Invite invite = await _context.Invites.FirstOrDefaultAsync(i => i.CompanyToken == token);
            if (invite == null)
            {
                return false;
            }
            try
            {
                invite.IsValid = true;
                invite.InviteeId = userId;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> AddNewInviteAsync(Invite invite)
        {
            try
            {
                await _context.Invites.AddAsync(invite);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> AnyInviteAsync(Guid token, string email, int companyId)
        {
            try
            {
                bool result = await _context.Invites.Where(i => i.CompanyId == companyId).AnyAsync(i => i.CompanyToken == token && i.InviteeEmail == email);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Invite> GetInviteAsync(int inviteId, int companyId)
        {
            try
            {
                Invite invite = await _context.Invites.Where(i => i.CompanyId == companyId)
                    .Include(i => i.Company)
                    .Include(i => i.Project)
                    .Include(i => i.Invitor).FirstOrDefaultAsync(i => i.Id == inviteId);
                return invite;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> ValidateInviteCodeAsync(Guid? token)
        {
            if (token == null)
            {
                return false;
            }
            bool result = false;
            Invite invite = await _context.Invites.FirstOrDefaultAsync(i => i.CompanyToken == token);
            if (invite != null)
            {
                DateTime inviteDate = invite.InviteDate.DateTime;
                bool validDate = (DateTime.Now - inviteDate).TotalDays <= 7;
                if (validDate)
                {
                    result = invite.IsValid;
                }
            }
            return result;
        }
    }
}
