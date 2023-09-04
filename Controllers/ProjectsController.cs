using BTAnshDesai.Extensions;
using BTAnshDesai.Models;
using BTAnshDesai.Models.enums;
using BTAnshDesai.Models.ViewModels;
using BTAnshDesai.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BTAnshDesai.Controllers
{
	[Authorize]
	public class ProjectsController : Controller
	{

		private readonly IBTRolesService _rolesService;
		private readonly IBTLookupService _lookupService;
		private readonly IBTFileService _fileService;
		private readonly IBTProjectService _projectService;
		private readonly UserManager<BTUser> _userManager;
		private readonly IBTCompanyInfoService _companyInfoService;
		public ProjectsController(IBTRolesService rolesService, IBTLookupService lookupService, IBTFileService fileService, IBTProjectService projectService, UserManager<BTUser> userManager, IBTCompanyInfoService companyInfoService)
		{

			_rolesService = rolesService;
			_lookupService = lookupService;
			_fileService = fileService;
			_projectService = projectService;
			_userManager = userManager;
			_companyInfoService = companyInfoService;
		}

		public async Task<IActionResult> MyProjects()
		{
			string userId = _userManager.GetUserId(User);
			List<Project> projects = await _projectService.GetUserProjectsAsync(userId);
			return (View(projects));
		}
		public async Task<IActionResult> AllProjects()
		{

			List<Project> projects = new();
			int companyId = User.Identity.GetCompanyId().Value;
			if (User.IsInRole(Roles.Admin.ToString()) || User.IsInRole(Roles.ProjectManager.ToString()))
			{

				projects = await _companyInfoService.GetAllProjectsAsync(companyId);
			}
			else
			{

			}
			return (View(projects));
		}

		public async Task<IActionResult> ArchivedProjects()
		{
			int companyId = User.Identity.GetCompanyId().Value;
			List<Project> projects = await _projectService.GetArchivedProjectsByCompany(companyId);
			return (View(projects));
		}
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> UnassignedProjects()
		{
			int companyId = User.Identity.GetCompanyId().Value;
			List<Project> projects = await _projectService.GetUnassignedProjectsAsync(companyId);
			return (View(projects));
		}
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AssignPM(int projectId)
		{
			int companyId = User.Identity.GetCompanyId().Value;
			AssignPMViewModel model = new();
			model.Project = await _projectService.GetProjectByIdAsync(projectId, companyId);
			model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
			return View(model);
		}
		[Authorize(Roles = "Admin")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AssignPM(AssignPMViewModel model)
		{
			if (!string.IsNullOrEmpty(model.PMID))
			{
				await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);
				return RedirectToAction("Details", new { id = model.Project.Id });
			}
			return RedirectToAction("AssignPM", new { projectId = model.Project.Id });
		}
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> AssignMembers(int id)
		{
			ProjectMembersViewModel model = new();
			int companyId = User.Identity.GetCompanyId().Value;
			model.Project = await _projectService.GetProjectByIdAsync(id, companyId);
			List<BTUser> developers = await _rolesService.GetUsersInRoleAsync(Roles.Developer.ToString(), companyId);
			List<BTUser> submitters = await _rolesService.GetUsersInRoleAsync(Roles.Submitter.ToString(), companyId);
			List<BTUser> companyMembers = developers.Concat(submitters).ToList();
			List<string> members = model.Project.Members.Select(m => m.Id).ToList();
			model.Users = new MultiSelectList(members, "Id", "FullName");
			return View(model);
		}
		[Authorize(Roles = "Admin, ProjectManager")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AssignMembers(ProjectMembersViewModel model)
		{
			if (model.SelectedUsers != null)
			{
				List<string> members = (await _projectService.GetAllProjectMembersExceptPMAsync(model.Project.Id)).Select(m => m.Id).ToList();
				foreach (string member in members)
				{
					await _projectService.RemoveUserFromProjectAsync(member, model.Project.Id);
				}
				foreach (string member in model.SelectedUsers)
				{
					await _projectService.AddUserToProjectAsync(member, model.Project.Id);
				}
				return RedirectToAction("Details", "Projects", new { id = model.Project.Id });
			}
			return RedirectToAction("AssignMembers", new { id = model.Project.Id });
		}
		// GET: Projects/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			// Remember that the _context should not be used directly in the controller so....     

			// Edit the following code to use the service layer. 
			// Your goal is to return the 'project' from the databse
			// with the Id equal to the parameter passed in.               
			// This is the only modification necessary for this method/action.
			int companyId = User.Identity.GetCompanyId().Value;
			Project project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

			if (project == null)
			{
				return NotFound();
			}

			return View(project);
		}
		[Authorize(Roles = "Admin, ProjectManager")]
		// GET: Projects/Create
		public async Task<IActionResult> Create()
		{
			int companyId = User.Identity.GetCompanyId().Value;
			AddProjectWithPMViewModel model = new();
			model.Project = new Project();
			model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
			model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "Id", "Name");
			return View(model);
		}

		// POST: Projects/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[Authorize(Roles = "Admin, ProjectManager")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
		{
			if (model != null)
			{
				int companyId = User.Identity.GetCompanyId().Value;
				try
				{
					if (model.Project.ImageFormFile != null)
					{
						model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
						model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
						model.Project.ImageFileContentType = model.Project.ImageFormFile.ContentType;
					}
					model.Project.CompanyId = companyId;
					model.Project.StartDate = model.Project.StartDate.ToUniversalTime();
					model.Project.EndDate = model.Project.EndDate.ToUniversalTime();
					await _projectService.AddNewProjectAsync(model.Project);
					if (!string.IsNullOrEmpty(model.PmId))
					{
						await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
					}

				}
				catch (Exception ex)
				{
					throw;
				}
				return RedirectToAction("Index");
			}

			return RedirectToAction("Create");
		}

		// GET: Projects/Edit/5
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> Edit(int? id)
		{
			int companyId = User.Identity.GetCompanyId().Value;
			AddProjectWithPMViewModel model = new();
			model.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);
			model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
			model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "Id", "Name");
			return View(model);
		}

		// POST: Projects/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[Authorize(Roles = "Admin, ProjectManager")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(AddProjectWithPMViewModel model)
		{
			if (model != null)
			{
				try
				{
					if (model.Project.ImageFormFile != null)
					{
						model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
						model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
						model.Project.ImageFileContentType = model.Project.ImageFormFile.ContentType;
					}
					model.Project.StartDate = model.Project.StartDate.ToUniversalTime();
					model.Project.EndDate = model.Project.EndDate.ToUniversalTime();
					await _projectService.UpdateProjectAsync(model.Project);
					if (!string.IsNullOrEmpty(model.PmId))
					{
						await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
					}
					return RedirectToAction("Index");
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!await ProjectExistsAsync(model.Project.Id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}

			}
			return RedirectToAction("Edit");
		}



		// GET: Projects/Archive/5
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> Archive(int? id)
		{

			int companyId = User.Identity.GetCompanyId().Value;
			var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);
			if (project == null)
			{
				return NotFound();
			}

			return View(project);
		}

		// POST: Projects/Archive/5
		[Authorize(Roles = "Admin, ProjectManager")]
		[HttpPost, ActionName("Archive")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ArchiveConfirmed(int id)
		{
			int companyId = User.Identity.GetCompanyId().Value;
			var project = await _projectService.GetProjectByIdAsync(id, companyId);
			_projectService.ArchiveProjectAsync(project);
			return RedirectToAction(nameof(Index));
		}
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> Restore(int? id)
		{
			int companyId = User.Identity.GetCompanyId().Value;
			var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);
			if (project == null)
			{
				return NotFound();
			}

			return View(project);
		}
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> RestoreConfirmed(int id)
		{
			int companyId = User.Identity.GetCompanyId().Value;
			var project = await _projectService.GetProjectByIdAsync(id, companyId);
			await _projectService.RestoreProjectAsync(project);
			return RedirectToAction(nameof(Index));
		}
		private async Task<bool> ProjectExistsAsync(int id)
		{
			int companyId = User.Identity.GetCompanyId().Value;
			return (await _projectService.GetAllProjectsByCompany(companyId)).Any(p => p.Id == id);
		}
	}
}
