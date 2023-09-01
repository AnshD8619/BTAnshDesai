using Microsoft.AspNetCore.Mvc.Rendering;

namespace BTAnshDesai.Models.ViewModels
{
    public class ManageUserRolesViewModel
    {
        public BTUser User { get; set; }
        public MultiSelectList Roles { get; set; }
        public List<string> SelectedRoles { get; set; }
    }
}
