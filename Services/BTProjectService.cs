using BTAnshDesai.Data;
using BTAnshDesai.Models;
using BTAnshDesai.Models.enums;
using BTAnshDesai.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BTAnshDesai.Services
{
    public class BTProjectService : IBTProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTRolesService _rolesService;

        public BTProjectService(ApplicationDbContext context, IBTRolesService rolesService)
        {
            _context = context;
            _rolesService = rolesService;
            ;
        }

        public async Task AddNewProjectAsync(Project project)
        {
            _context.Add(project);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AddProjectManagerAsync(string userId, int projectId)
        {
            BTUser currentPM = await GetProjectManagerAsync(projectId);
            if (currentPM != null)
            {
                try
                {
                    await RemoveProjectManagerAsync(projectId);
                }
                catch (Exception ex)
                {
                    throw;
                    return false;
                }
            }

            try
            {
                await AddUserToProjectAsync(userId, projectId);
                return true;
            }
            catch (Exception ex)
            {
                throw;
                return false;
            }
        }

        public async Task<bool> AddUserToProjectAsync(string userId, int projectId)
        {
            BTUser user = await _context.Users.FirstOrDefaultAsync(p => p.Id == userId);
            if (user != null)
            {
                Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if (!await IsUserOnProjectAsync(userId, projectId))
                {
                    try
                    {
                        project.Members.Add(user);
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                }
            }

            return false;
        }

        public async Task ArchiveProjectAsync(Project project)
        {
            project.Archived = true;
            await UpdateProjectAsync(project);
            foreach (Ticket ticket in project.Tickets)
            {
                ticket.ArchivedByProject = true;
                _context.Update(ticket);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<BTUser>> GetAllProjectMembersExceptPMAsync(int projectId)
        {
            List<BTUser> developers = await GetProjectMembersByRoleAsync(projectId, Roles.Developer.ToString());
            List<BTUser> submitters = await GetProjectMembersByRoleAsync(projectId, Roles.Submitter.ToString());
            List<BTUser> admins = await GetProjectMembersByRoleAsync(projectId, Roles.Admin.ToString());
            List<BTUser> members = developers.Concat(submitters).Concat(admins).ToList();
            return members;
        }

        public async Task<List<Project>> GetAllProjectsByCompany(int companyId)
        {
            List<Project> projects = new();
            projects = await _context.Projects.Where(p => p.CompanyId == companyId)
                .Include(p => p.Members)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.Comments)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.Attachments)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.History)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.Notifications)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.DeveloperUser)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.OwnerUser)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.TicketStatus)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.TicketPriority)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.TicketType)
                .Include(p => p.ProjectPriority).ToListAsync();

            return projects;
        }

        public async Task<List<Project>> GetAllProjectsByPriority(int companyId, string priorityName)
        {
            List<Project> projects = await GetAllProjectsByCompany(companyId);
            int priorityId = await LookupProjectPriorityId(priorityName);
            return projects.Where(p => p.Id == priorityId).ToList();
        }

        public async Task<List<Project>> GetArchivedProjectsByCompany(int companyId)
        {
            List<Project> projects = await GetAllProjectsByCompany(companyId);


            return projects.Where(p => p.Archived == true).ToList();

        }

        public async Task<List<BTUser>> GetDevelopersOnProjectAsync(int projectId)
        {
            List<BTUser> developers = await GetProjectMembersByRoleAsync(projectId, Roles.Developer.ToString());
            return developers;
        }

        public async Task<Project> GetProjectByIdAsync(int projectId, int companyId)
        {
            Project project = await _context.Projects
                .Include(p => p.Tickets)
                .ThenInclude(t => t.TicketPriority)
                .Include(p => p.Tickets)
                .ThenInclude(t => t.TicketStatus)
                .Include(p => p.Tickets)
                .ThenInclude(t => t.TicketType)
                .Include(p => p.Tickets)
                .ThenInclude(t => t.DeveloperUser)
                .Include(p => p.Tickets)
                .ThenInclude(t => t.OwnerUser)
                .Include(p => p.Members)
                .Include(p => p.ProjectPriority)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);
            return project;
        }

        public async Task<BTUser> GetProjectManagerAsync(int projectId)
        {
            Project project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);
            foreach (BTUser user in project?.Members)
            {
                if (await _rolesService.IsUserInRoleAsync(user, Roles.ProjectManager.ToString()))
                {
                    return user;
                }
            }
            return null;
        }

        public async Task<List<BTUser>> GetProjectMembersByRoleAsync(int projectId, string role)
        {
            Project project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);
            List<BTUser> members = new();
            foreach (var user in project.Members)
            {
                if (await _rolesService.IsUserInRoleAsync(user, role))
                {
                    members.Add(user);
                }
            }
            return members;
        }

        public Task<List<BTUser>> GetSubmittersOnProjectAsync(int projectId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Project>> GetUserProjectsAsync(string userId)
        {
            try
            {
                List<Project> projects = (await _context.Users
                .Include(u => u.Projects)
                    .ThenInclude(p => p.Company)
                .Include(u => u.Projects)
                    .ThenInclude(p => p.Members)
                .Include(p => p.Projects)
                    .ThenInclude(p => p.Tickets)
                .Include(p => p.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.DeveloperUser)
                .Include(p => p.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.OwnerUser)
                .Include(p => p.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.DeveloperUser)
                .Include(p => p.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
                .Include(p => p.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.TicketStatus)
                .Include(p => p.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.TicketType)
                .FirstOrDefaultAsync(u => u.Id == userId)).Projects.ToList();
                return projects;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<BTUser>> GetUsersNotOnProjectAsync(int projectId, int companyId)
        {
            List<BTUser> users = await _context.Users.Where(u => u.Projects.All(p => p.Id != projectId)).ToListAsync();
            return users.Where(u => u.CompanyId == companyId).ToList();
        }

        public async Task<bool> IsAssignedProjectManager(string userId, int projectId)
        {
            try
            {
                string projectManager = (await GetProjectManagerAsync(projectId))?.Id;
                if (projectManager == userId) 
                { 
                    return true; 
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> IsUserOnProjectAsync(string userId, int projectId)
        {
            Project project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);
            bool result = false;
            if (project != null)
            {
                result = project.Members.Any(u => u.Id == userId);
            }
            return result;
        }

        public async Task<int> LookupProjectPriorityId(string priorityName)
        {
            int priorityId = (await _context.ProjectPriorities.FirstOrDefaultAsync(p => p.Name == priorityName)).Id;
            return priorityId;
        }

        public async Task RemoveProjectManagerAsync(int projectId)
        {
            Project project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);
            try
            {
                foreach (BTUser user in project.Members)
                {
                    if (await _rolesService.IsUserInRoleAsync(user, Roles.ProjectManager.ToString()))
                    {
                        await RemoveUserFromProjectAsync(user.Id, projectId);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task RemoveUserFromProjectAsync(string userId, int projectId)
        {
            try
            {
                BTUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                try
                {
                    if (await IsUserOnProjectAsync(userId, projectId))
                    {
                        project.Members.Remove(user);
                        await _context.SaveChangesAsync();

                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task RemoveUsersFromProjectByRoleAsync(string role, int projectId)
        {
            try
            {
                List<BTUser> members = await GetProjectMembersByRoleAsync(projectId, role);
                Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                foreach (BTUser user in members)
                {
                    try
                    {
                        project.Members.Remove(user);
                        await _context.SaveChangesAsync();

                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task RestoreProjectAsync(Project project)
        {
            project.Archived = false;
            await UpdateProjectAsync(project);
            foreach (Ticket ticket in project.Tickets)
            {
                ticket.ArchivedByProject = false;
                _context.Update(ticket);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateProjectAsync(Project project)
        {
            _context.Update(project);
            await _context.SaveChangesAsync();
        }
    }
}
