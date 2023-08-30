
using BTAnshDesai.Controllers.Interfaces;
using BTAnshDesai.Data;
using BTAnshDesai.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace BTAnshDesai.Services
{
    public class BTNotificationService : IBTNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IBTRolesService _rolesService;

        public BTNotificationService(ApplicationDbContext context, IEmailSender emailSender, IBTRolesService rolesService)
        {
            _context = context;
            _emailSender = emailSender;
            _rolesService = rolesService;
        }
        public async Task AddNotificationAsync(Notification notification)
        {
            try
            {
                await _context.AddAsync(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<Notification>> GetReceivedNotificationsAsync(string userId)
        {
            try
            {
                List<Notification> notifications = await _context.Notifications
                    .Include(n => n.Recipient)
                    .Include(n => n.Sender)
                    .Include(n => n.Ticket)
                        .ThenInclude(t => t.Project)
                    .Where(n => n.RecipientId == userId).ToListAsync();
                return notifications;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<Notification>> GetSentNotificationsAsync(string userId)
        {
            try
            {
                List<Notification> notifications = await _context.Notifications
                    .Include(n => n.Recipient)
                    .Include(n => n.Sender)
                    .Include(n => n.Ticket)
                        .ThenInclude(t => t.Project)
                    .Where(n => n.SenderId == userId).ToListAsync();
                return notifications;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> SendEmailNotificationAsync(Notification notification, string emailSubject)
        {
            BTUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == notification.RecipientId);
            if (user != null)
            {
                string btUserEmail = user.Email;
                string message = notification.Message;
                try
                {
                    await _emailSender.SendEmailAsync(btUserEmail, emailSubject, message);
                    return true;
                }
                catch (Exception ex)
                {
                    throw;
                }

            }
            return false;
        }

        public async Task SendEmailNotificationsByRoleAsync(Notification notification, int companyId, string role)
        {
            try
            {
                List<BTUser> members = await _rolesService.GetUsersInRoleAsync(role, companyId);
                foreach (BTUser user in members)
                {
                    notification.RecipientId = user.Id;
                    await SendEmailNotificationAsync(notification, notification.Title);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task SendMembersEmailNotificationsAsync(Notification notification, List<BTUser> members)
        {
            try
            {
                foreach (BTUser user in members)
                {
                    notification.RecipientId = user.Id;
                    await SendEmailNotificationAsync(notification, notification.Title);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
