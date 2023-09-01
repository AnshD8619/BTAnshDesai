using BTAnshDesai.Extensions;
using BTAnshDesai.Models;
using BTAnshDesai.Models.ViewModels;
using BTAnshDesai.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTAnshDesai.Controllers
{
    [Authorize]
    public class UserRolesController : Controller
    {
        private readonly IBTRolesService _rolesService;
        private readonly IBTCompanyInfoService _companyInfoService;

        public UserRolesController(IBTRolesService rolesService, IBTCompanyInfoService companyInfoService)
        {
            _rolesService = rolesService;
            _companyInfoService = companyInfoService;
        }
        [HttpGet]
        public async Task<IActionResult> ManageUserRoles()
        {
            List<ManageUserRolesViewModel> model = new();
            int companyId = User.Identity.GetCompanyId().Value;
            List<BTUser> users = await _companyInfoService.GetAllMembersAsync(companyId);
            foreach (BTUser user in users)
            {
                ManageUserRolesViewModel viewModel = new();
                viewModel.User = user;
                IEnumerable<string> selected = await _rolesService.GetUserRolesAsync(user);
                viewModel.Roles = new Microsoft.AspNetCore.Mvc.Rendering.MultiSelectList(await _rolesService.GetRolesAsync(), "Name", "Name", selected);
                model.Add(viewModel);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel member)
        {
            int companyId = User.Identity.GetCompanyId().Value;
            BTUser user = (await _companyInfoService.GetAllMembersAsync(companyId)).FirstOrDefault(u => u.Id == member.User.Id);
            IEnumerable<string> roles = await _rolesService.GetUserRolesAsync(user);
            string userRole = member.SelectedRoles.FirstOrDefault();
            if (string.IsNullOrEmpty(userRole))
            {
                if (await _rolesService.RemoveUserFromRolesAsync(user, roles))
                {
                    await _rolesService.AddUserToRoleAsync(user, userRole);
                }
            }
            return RedirectToAction(nameof(ManageUserRoles));
        }
    }
}
