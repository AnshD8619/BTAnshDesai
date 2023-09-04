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
	public class TicketsController : Controller
	{

		private readonly UserManager<BTUser> _userManager;
		private readonly IBTProjectService _projectService;
		private readonly IBTLookupService _lookupService;
		private readonly IBTTicketService _ticketService;
		private readonly IBTFileService _fileService;
		private readonly IBTTicketHistoryService _historyService;
		public TicketsController(Microsoft.AspNetCore.Identity.UserManager<BTUser> userManager, IBTProjectService projectService, IBTLookupService lookupService, IBTTicketService ticketService, IBTFileService fileService, IBTTicketHistoryService historyService)
		{

			_userManager = userManager;
			_projectService = projectService;
			_lookupService = lookupService;
			_ticketService = ticketService;
			_fileService = fileService;
			_historyService = historyService;
		}

		// GET: Tickets
		public async Task<IActionResult> MyTickets()
		{
			BTUser user = await _userManager.GetUserAsync(User);
			List<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(user.Id, user.CompanyId);
			return View(tickets);
		}
		// GET: Tickets
		public async Task<IActionResult> AllTickets()
		{
			int companyId = User.Identity.GetCompanyId().Value;
			List<Ticket> tickets = await _ticketService.GetAllTicketsByCompanyAsync(companyId);
			if (User.IsInRole(Roles.DemoUser.ToString()) || User.IsInRole(Roles.Submitter.ToString()))
			{
				return View(tickets.Where(t => t.Archived = false));
			}
			else
			{
				return View(tickets);
			}
		}
		public async Task<IActionResult> ArchivedTickets()
		{
			int companyId = User.Identity.GetCompanyId().Value;
			List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(companyId);
			return View(tickets);

		}
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> UnassignedTickets()
		{
			int companyId = User.Identity.GetCompanyId().Value;
			string userId = _userManager.GetUserId(User);
			List<Ticket> tickets = await _ticketService.GetUnassignedTicketsAsync(companyId);
			if (User.IsInRole(Roles.Admin.ToString()))
			{
				return View(tickets);
			}
			else
			{
				List<Ticket> filteredTickets = new();
				foreach (Ticket ticket in tickets)
				{
					if (await _projectService.IsAssignedProjectManager(userId, ticket.ProjectId) == true)
					{
						filteredTickets.Add(ticket);
					}
				}
				return View(filteredTickets);
			}


		}
		// GET: Tickets/Details/5
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> AssignDeveloper(int id)
		{
			AssignDeveloperViewModel model = new();
			model.Ticket = await _ticketService.GetTicketByIdAsync(id);
			model.Developers = new SelectList(await _projectService.GetProjectMembersByRoleAsync(model.Ticket.ProjectId, Roles.Developer.ToString()), "Id", "FullName");
			return View(model);
		}
		[Authorize(Roles = "Admin, ProjectManager")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel model)
		{
			if (model.DeveloperId != null)
			{
				BTUser user = await _userManager.GetUserAsync(User);
				Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);
				try
				{
					await _ticketService.AssignTicketAsync(model.Ticket.Id, model.DeveloperId);
				}
				catch (Exception ex)
				{
					throw;
				}
				Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);
				await _historyService.AddHistoryAsync(oldTicket, newTicket, user.Id);
				return RedirectToAction("Details", new { id = model.Ticket.Id });
			}
			return RedirectToAction("AssignDeveloper", new { id = model.Ticket.Id });
		}
		public async Task<IActionResult> Details(int? id)
		{



			Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);
			if (ticket == null)
			{
				return NotFound();
			}

			return View(ticket);
		}

		// GET: Tickets/Create
		public async Task<IActionResult> Create()
		{
			BTUser user = await _userManager.GetUserAsync(User);
			int companyId = User.Identity.GetCompanyId().Value;
			if (User.IsInRole(Roles.Admin.ToString()))
			{
				ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompany(companyId), "Id", "Name");
			}
			else
			{
				ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(user.Id), "Id", "Name");
			}

			ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
			ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Name", "Id");
			return View();
		}

		// POST: Tickets/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId,")] Ticket ticket)
		{
			BTUser user = await _userManager.GetUserAsync(User);
			if (ModelState.IsValid)
			{
				ticket.Created = DateTimeOffset.UtcNow;
				ticket.OwnerUserId = user.Id;
				ticket.TicketStatusId = (await _ticketService.LookupTicketStatusIdAsync(BTTicketStatus.New.ToString())).Value;
				await _ticketService.AddNewTicketAsync(ticket);
				Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
				await _historyService.AddHistoryAsync(null, newTicket, user.Id);
				return RedirectToAction(nameof(Index));
			}
			if (User.IsInRole(Roles.Admin.ToString()))
			{
				ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompany(user.CompanyId), "Id", "Name");
			}
			else
			{
				ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(user.Id), "Id", "Name");
			}
			ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
			ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
			return View(ticket);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddTicketComment(int id, [Bind("Id, TicketId, Comment")] TicketComment comment)
		{
			if (ModelState.IsValid)
			{
				try
				{
					comment.UserId = _userManager.GetUserId(User);
					comment.Created = DateTimeOffset.UtcNow;
					await _ticketService.AddTicketCommentAsync(comment);
					await _historyService.AddHistoryAsync(comment.TicketId, "TicketComment", comment.UserId);

				}
				catch (Exception ex)
				{
					throw;
				}

			}
			return RedirectToAction("Details", new { id = comment.TicketId });
		}
		// GET: Tickets/Edit/5       
		public async Task<IActionResult> Edit(int? id)
		{


			Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);
			if (ticket == null)
			{
				return NotFound();
			}
			ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
			ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
			ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);
			return View(ticket);
		}

		// POST: Tickets/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,DeveloperUserId")] Ticket ticket)
		{

			if (id != ticket.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				BTUser user = await _userManager.GetUserAsync(User);
				Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);

				try
				{
					ticket.Updated = DateTimeOffset.UtcNow;
					await _ticketService.UpdateTicketAsync(ticket);
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!await TicketExists(ticket.Id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
				await _historyService.AddHistoryAsync(oldTicket, newTicket, user.Id);
				return RedirectToAction(nameof(Index));
			}
			ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
			ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
			ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);
			return View(ticket);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
		{
			string statusMessage;

			if (ModelState.IsValid && ticketAttachment.FormFile != null)
			{
				ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
				ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
				ticketAttachment.FileContentType = ticketAttachment.FormFile.ContentType;

				ticketAttachment.Created = DateTimeOffset.UtcNow;
				ticketAttachment.UserId = _userManager.GetUserId(User);

				await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
				await _historyService.AddHistoryAsync(ticketAttachment.TicketId, "TicketAttachment", ticketAttachment.UserId);

				statusMessage = "Success: New attachment added to Ticket.";
			}
			else
			{
				statusMessage = "Error: Invalid data.";

			}

			return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
		}
		// GET: Tickets/Archive/5
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> Archive(int? id)
		{


			Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);
			if (ticket == null)
			{
				return NotFound();
			}

			return View(ticket);
		}

		// POST: Tickets/Delete/5
		[Authorize(Roles = "Admin, ProjectManager")]
		[HttpPost, ActionName("Archive")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ArchiveConfirmed(int id)
		{

			Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
			ticket.Archived = true;
			await _ticketService.UpdateTicketAsync(ticket);
			return RedirectToAction("Index");



		}
		// GET: Tickets/Restore/5
		[Authorize(Roles = "Admin, ProjectManager")]
		public async Task<IActionResult> Restore(int? id)
		{


			Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);
			if (ticket == null)
			{
				return NotFound();
			}

			return View(ticket);
		}

		// POST: Tickets/Delete/5
		[Authorize(Roles = "Admin, ProjectManager")]
		[HttpPost, ActionName("Restore")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RestoreConfirmed(int id)
		{

			Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
			ticket.Archived = false;
			await _ticketService.UpdateTicketAsync(ticket);
			return RedirectToAction("Index");



		}

		private async Task<bool> TicketExists(int id)
		{
			int companyId = User.Identity.GetCompanyId().Value;
			return (await _ticketService.GetAllTicketsByCompanyAsync(companyId)).Any(t => t.Id == id);
		}
	}
}
