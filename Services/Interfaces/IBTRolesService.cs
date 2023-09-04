using BTAnshDesai.Models;
using Microsoft.AspNetCore.Identity;

namespace BTAnshDesai.Services.Interfaces
{
	public interface IBTRolesService
	{
		public Task<bool> IsUserInRoleAsync(BTUser user, string roleName);
		public Task<IEnumerable<string>> GetUserRolesAsync(BTUser user);
		public Task<bool> AddUserToRoleAsync(BTUser user, string roleName);
		public Task<bool> RemoveUserFromRoleAsync(BTUser user, string roles);
		public Task<bool> RemoveUserFromRolesAsync(BTUser user, IEnumerable<string> roles);
		public Task<List<BTUser>> GetUsersInRoleAsync(string roleName, int companyId);
		public Task<List<BTUser>> GetUsersNotInRoleAsync(string roleName, int companyId);
		public Task<string> getRoleNameByIdAsync(string roleId);
		public Task<List<IdentityRole>> GetRolesAsync();

	}
}
