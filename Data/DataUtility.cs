using BTAnshDesai.Models;
using BTAnshDesai.Models.enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BTAnshDesai.Data
{
	public static class DataUtility
	{
		//Company Ids
		private static int company1Id;

		public static string GetConnectionString(IConfiguration configuration)
		{
			//The default connection string will come from appSettings like usual
			var connectionString = configuration.GetConnectionString("DefaultConnection");
			//It will be automatically overwritten if we are running on Heroku
			var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
			return string.IsNullOrEmpty(databaseUrl) ? connectionString : BuildConnectionString(databaseUrl);
		}

		public static string BuildConnectionString(string databaseUrl)
		{
			//Provides an object representation of a uniform resource identifier (URI) and easy access to the parts of the URI.
			var databaseUri = new Uri(databaseUrl);
			var userInfo = databaseUri.UserInfo.Split(':');
			//Provides a simple way to create and manage the contents of connection strings used by the NpgsqlConnection class.
			var builder = new NpgsqlConnectionStringBuilder
			{
				Host = databaseUri.Host,
				Port = databaseUri.Port,
				Username = userInfo[0],
				Password = userInfo[1],
				Database = databaseUri.LocalPath.TrimStart('/'),
				SslMode = SslMode.Prefer,
				TrustServerCertificate = true
			};
			return builder.ToString();
		}

		public static async Task ManageDataAsync(IHost host)
		{
			using var svcScope = host.Services.CreateScope();
			var svcProvider = svcScope.ServiceProvider;
			//Service: An instance of RoleManager
			var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
			//Service: An instance of RoleManager
			var roleManagerSvc = svcProvider.GetRequiredService<RoleManager<IdentityRole>>();
			//Service: An instance of the UserManager
			var userManagerSvc = svcProvider.GetRequiredService<UserManager<BTUser>>();
			//Migration: This is the programmatic equivalent to Update-Database
			await dbContextSvc.Database.MigrateAsync();


			//Custom  Bug Tracker Seed Methods
			await SeedRolesAsync(roleManagerSvc);
			await SeedDefaultCompaniesAsync(dbContextSvc);
			await SeedDefaultUsersAsync(userManagerSvc);
			
			await SeedDefaultTicketTypeAsync(dbContextSvc);
			await SeedDefaultTicketStatusAsync(dbContextSvc);
			await SeedDefaultTicketPriorityAsync(dbContextSvc);
			await SeedDefaultProjectPriorityAsync(dbContextSvc);
			await SeedDefaultProjectsAsync(dbContextSvc);
			await SeedDefaultTicketsAsync(dbContextSvc);
		}


		public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
		{
			//Seed Roles
			await roleManager.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
			await roleManager.CreateAsync(new IdentityRole(Roles.ProjectManager.ToString()));
			await roleManager.CreateAsync(new IdentityRole(Roles.Developer.ToString()));
			await roleManager.CreateAsync(new IdentityRole(Roles.Submitter.ToString()));
			await roleManager.CreateAsync(new IdentityRole(Roles.DemoUser.ToString()));
		}

		public static async Task SeedDefaultCompaniesAsync(ApplicationDbContext context)
		{
			try
			{
				IList<Company> defaultcompanies = new List<Company>() {
					new Company() { Name = "Demo Company", Description="This is the demo Company" },					
				};

				var dbCompanies = context.Companies.Select(c => c.Name).ToList();
				await context.Companies.AddRangeAsync(defaultcompanies.Where(c => !dbCompanies.Contains(c.Name)));
				await context.SaveChangesAsync();

				//Get company Ids
				company1Id = context.Companies.FirstOrDefault(p => p.Name == "Demo Company").Id;				
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Companies.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
		}

		public static async Task SeedDefaultProjectPriorityAsync(ApplicationDbContext context)
		{
			try
			{
				IList<Models.ProjectPriority> projectPriorities = new List<ProjectPriority>() {
													new ProjectPriority() { Name = BTProjectPriority.Low.ToString() },
													new ProjectPriority() { Name = BTProjectPriority.Medium.ToString() },
													new ProjectPriority() { Name = BTProjectPriority.High.ToString() },
													new ProjectPriority() { Name = BTProjectPriority.Urgent.ToString() },
				};

				var dbProjectPriorities = context.ProjectPriorities.Select(c => c.Name).ToList();
				await context.ProjectPriorities.AddRangeAsync(projectPriorities.Where(c => !dbProjectPriorities.Contains(c.Name)));
				await context.SaveChangesAsync();

			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Project Priorities.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
		}

		public static async Task SeedDefaultProjectsAsync(ApplicationDbContext context)
		{

			//Get project priority Ids
			int priorityLow = context.ProjectPriorities.FirstOrDefault(p => p.Name == BTProjectPriority.Low.ToString()).Id;
			int priorityMedium = context.ProjectPriorities.FirstOrDefault(p => p.Name == BTProjectPriority.Medium.ToString()).Id;
			int priorityHigh = context.ProjectPriorities.FirstOrDefault(p => p.Name == BTProjectPriority.High.ToString()).Id;
			int priorityUrgent = context.ProjectPriorities.FirstOrDefault(p => p.Name == BTProjectPriority.Urgent.ToString()).Id;

			try
			{
				IList<Project> projects = new List<Project>() {
					 new Project()
					 {
						 CompanyId = company1Id,
						 Name = "Personal Porfolio",
						 Description="Single page html, css & javascript page.  Serves as a landing page for candidates and contains a bio and links to all applications and challenges." ,
						 StartDate = DateTime.SpecifyKind(new DateTime(2024,8,20), DateTimeKind.Utc),
						 EndDate = DateTime.SpecifyKind(new DateTime(2024,9,20).AddMonths(1), DateTimeKind.Utc),
						 ProjectPriorityId = priorityLow,
					 },
					 new Project()
					 {
						 CompanyId = company1Id,
						 Name = "Blog Web Application",
						 Description="Candidate's custom built web application using .Net Core with MVC, a postgres database and hosted in a heroku container.  The app is designed for the candidate to create, update and maintain a live blog site.",
						 StartDate = DateTime.SpecifyKind(new DateTime(2023,9,20), DateTimeKind.Utc),
						 EndDate = DateTime.SpecifyKind(new DateTime(2024,9,20).AddMonths(4), DateTimeKind.Utc),
						 ProjectPriorityId = priorityMedium
					 },
					 new Project()
					 {
						 CompanyId = company1Id,
						 Name = "Issue Tracking Web Application",
						 Description="A custom designed .Net Core application with postgres database.  The application is a multi tennent application designed to track issue tickets' progress.  Implemented with identity and user roles, Tickets are maintained in projects which are maintained by users in the role of projectmanager.  Each project has a team and team members.",
						 StartDate = DateTime.SpecifyKind(new DateTime(2023,8,20), DateTimeKind.Utc),
						 EndDate = DateTime.SpecifyKind(new DateTime(2024,8,20).AddMonths(6), DateTimeKind.Utc),
						 ProjectPriorityId = priorityHigh
					 },
					 new Project()
					 {
						 CompanyId = company1Id,
						 Name = "Address Book Web Application",
						 Description="A custom designed .Net Core application with postgres database.  This is an application to serve as a rolodex of contacts for a given user..",
						 StartDate = DateTime.SpecifyKind(new DateTime(2024,8,20), DateTimeKind.Utc),
						 EndDate = DateTime.SpecifyKind(new DateTime(2025,8,20).AddMonths(2), DateTimeKind.Utc),
						 ProjectPriorityId = priorityLow
					 },
					new Project()
					 {
						 CompanyId = company1Id,
						 Name = "Movie Information Web Application",
						 Description="A custom designed .Net Core application with postgres database.  An API based application allows users to input and import movie posters and details including cast and crew information.",
						 StartDate = DateTime.SpecifyKind(new DateTime(2021,8,20), DateTimeKind.Utc),
						 EndDate = DateTime.SpecifyKind(new DateTime(2025,8,20).AddMonths(3), DateTimeKind.Utc),
						 ProjectPriorityId = priorityHigh
					 }
				};

				var dbProjects = context.Projects.Select(c => c.Name).ToList();
				await context.Projects.AddRangeAsync(projects.Where(c => !dbProjects.Contains(c.Name)));
				await context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Projects.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
		}


		public static async Task SeedDefaultUsersAsync(UserManager<BTUser> userManager)
		{
			//Seed Demo Admin
			var defaultUser = new BTUser
			{
				UserName = "demoadmin@gmail.com",
				Email = "demoadmin@gmail.com",
				FirstName = "Demo",
				SurName = "Admin",
				EmailConfirmed = true,
				CompanyId = company1Id
			};
			try
			{
				var user = await userManager.FindByEmailAsync(defaultUser.Email);
				if (user == null)
				{
					await userManager.CreateAsync(defaultUser, "Abc&123!");
					await userManager.AddToRoleAsync(defaultUser, Roles.Admin.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Default Admin User.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
			//Seed Demo Project Manager
			defaultUser = new BTUser
			{
				UserName = "demoprojectmanager@gmail.com",
				Email = "demoprojectmanager@gmail.com",
				FirstName = "Demo",
				SurName = "Project Manager",
				EmailConfirmed = true,
				CompanyId = company1Id
			};
			try
			{
				var user = await userManager.FindByEmailAsync(defaultUser.Email);
				if (user == null)
				{
					await userManager.CreateAsync(defaultUser, "Abc&123!");
					await userManager.AddToRoleAsync(defaultUser, Roles.ProjectManager.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Default Admin User.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
			//Seed Demo Developer 1
			defaultUser = new BTUser
			{
				UserName = "demodeveloper1@gmail.com",
				Email = "demodeveloper1@gmail.com",
				FirstName = "Demo",
				SurName = "Developer 1",
				EmailConfirmed = true,
				CompanyId = company1Id
			};
			try
			{
				var user = await userManager.FindByEmailAsync(defaultUser.Email);
				if (user == null)
				{
					await userManager.CreateAsync(defaultUser, "Abc&123!");
					await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Default Admin User.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
			//Seed Demo Developer 2
			defaultUser = new BTUser
			{
				UserName = "demodeveloper2@gmail.com",
				Email = "demodeveloper2@gmail.com",
				FirstName = "Demo",
				SurName = "Developer 2",
				EmailConfirmed = true,
				CompanyId = company1Id
			};
			try
			{
				var user = await userManager.FindByEmailAsync(defaultUser.Email);
				if (user == null)
				{
					await userManager.CreateAsync(defaultUser, "Abc&123!");
					await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Default Admin User.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
			//Seed Demo Developer 3
			defaultUser = new BTUser
			{
				UserName = "demodeveloper3@gmail.com",
				Email = "demodeveloper3@gmail.com",
				FirstName = "Demo",
				SurName = "Developer 3",
				EmailConfirmed = true,
				CompanyId = company1Id
			};
			try
			{
				var user = await userManager.FindByEmailAsync(defaultUser.Email);
				if (user == null)
				{
					await userManager.CreateAsync(defaultUser, "Abc&123!");
					await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Default Admin User.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
			//Seed Demo Developer 4
			defaultUser = new BTUser
			{
				UserName = "demodeveloper4@gmail.com",
				Email = "demodeveloper4@gmail.com",
				FirstName = "Demo",
				SurName = "Developer 4",
				EmailConfirmed = true,
				CompanyId = company1Id
			};
			try
			{
				var user = await userManager.FindByEmailAsync(defaultUser.Email);
				if (user == null)
				{
					await userManager.CreateAsync(defaultUser, "Abc&123!");
					await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Default Admin User.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
			//Seed Demo Developer 5
			defaultUser = new BTUser
			{
				UserName = "demodeveloper5@gmail.com",
				Email = "demodeveloper5@gmail.com",
				FirstName = "Demo",
				SurName = "Developer 5",
				EmailConfirmed = true,
				CompanyId = company1Id
			};
			try
			{
				var user = await userManager.FindByEmailAsync(defaultUser.Email);
				if (user == null)
				{
					await userManager.CreateAsync(defaultUser, "Abc&123!");
					await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Default Admin User.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}			

		}	

		public static async Task SeedDefaultTicketTypeAsync(ApplicationDbContext context)
		{
			try
			{
				IList<TicketType> ticketTypes = new List<TicketType>() {
					 new TicketType() { Name = BTTicketType.NewDevelopment.ToString() },      // Ticket involves development of a new, uncoded solution 
                     new TicketType() { Name = BTTicketType.WorkTask.ToString() },            // Ticket involves development of the specific ticket description 
                     new TicketType() { Name = BTTicketType.Defect.ToString()},               // Ticket involves unexpected development/maintenance on a previously designed feature/functionality
                     new TicketType() { Name = BTTicketType.ChangeRequest.ToString() },       // Ticket involves modification development of a previously designed feature/functionality
                     new TicketType() { Name = BTTicketType.Enhancement.ToString() },         // Ticket involves additional development on a previously designed feature or new functionality
                     new TicketType() { Name = BTTicketType.GeneralTask.ToString() }          // Ticket involves no software development but may involve tasks such as configuations, or hardware setup
                };

				var dbTicketTypes = context.TicketTypes.Select(c => c.Name).ToList();
				await context.TicketTypes.AddRangeAsync(ticketTypes.Where(c => !dbTicketTypes.Contains(c.Name)));
				await context.SaveChangesAsync();

			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Ticket Types.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
		}

		public static async Task SeedDefaultTicketStatusAsync(ApplicationDbContext context)
		{
			try
			{
				IList<TicketStatus> ticketStatuses = new List<TicketStatus>() {
					new TicketStatus() { Name = BTTicketStatus.New.ToString() },                 // Newly Created ticket having never been assigned
                    new TicketStatus() { Name = BTTicketStatus.Development.ToString() },         // Ticket is assigned and currently being worked 
                    new TicketStatus() { Name = BTTicketStatus.Testing.ToString()  },            // Ticket is assigned and is currently being tested
                    new TicketStatus() { Name = BTTicketStatus.Resolved.ToString()  },           // Ticket remains assigned to the developer but work in now complete
                };

				var dbTicketStatuses = context.TicketStatuses.Select(c => c.Name).ToList();
				await context.TicketStatuses.AddRangeAsync(ticketStatuses.Where(c => !dbTicketStatuses.Contains(c.Name)));
				await context.SaveChangesAsync();

			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Ticket Statuses.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
		}

		public static async Task SeedDefaultTicketPriorityAsync(ApplicationDbContext context)
		{
			try
			{
				IList<TicketPriority> ticketPriorities = new List<TicketPriority>() {
													new TicketPriority() { Name = BTTicketPriority.Low.ToString()  },
													new TicketPriority() { Name = BTTicketPriority.Medium.ToString() },
													new TicketPriority() { Name = BTTicketPriority.High.ToString()},
													new TicketPriority() { Name = BTTicketPriority.Urgent.ToString()},
				};

				var dbTicketPriorities = context.TicketPriorities.Select(c => c.Name).ToList();
				await context.TicketPriorities.AddRangeAsync(ticketPriorities.Where(c => !dbTicketPriorities.Contains(c.Name)));
				context.SaveChanges();

			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Ticket Priorities.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
		}



		public static async Task SeedDefaultTicketsAsync(ApplicationDbContext context)
		{
			//Get project Ids
			int portfolioId = context.Projects.FirstOrDefault(p => p.Name == "Personal Porfolio").Id;
			int blogId = context.Projects.FirstOrDefault(p => p.Name == "Blog Web Application").Id;
			int bugtrackerId = context.Projects.FirstOrDefault(p => p.Name == "Issue Tracking Web Application").Id;
			int movieId = context.Projects.FirstOrDefault(p => p.Name == "Movie Information Web Application").Id;

			//Get ticket type Ids
			int typeNewDev = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.NewDevelopment.ToString()).Id;
			int typeWorkTask = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.WorkTask.ToString()).Id;
			int typeDefect = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.Defect.ToString()).Id;
			int typeEnhancement = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.Enhancement.ToString()).Id;
			int typeChangeRequest = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.ChangeRequest.ToString()).Id;

			//Get ticket priority Ids
			int priorityLow = context.TicketPriorities.FirstOrDefault(p => p.Name == BTTicketPriority.Low.ToString()).Id;
			int priorityMedium = context.TicketPriorities.FirstOrDefault(p => p.Name == BTTicketPriority.Medium.ToString()).Id;
			int priorityHigh = context.TicketPriorities.FirstOrDefault(p => p.Name == BTTicketPriority.High.ToString()).Id;
			int priorityUrgent = context.TicketPriorities.FirstOrDefault(p => p.Name == BTTicketPriority.Urgent.ToString()).Id;

			//Get ticket status Ids
			int statusNew = context.TicketStatuses.FirstOrDefault(p => p.Name == BTTicketStatus.New.ToString()).Id;
			int statusDev = context.TicketStatuses.FirstOrDefault(p => p.Name == BTTicketStatus.Development.ToString()).Id;
			int statusTest = context.TicketStatuses.FirstOrDefault(p => p.Name == BTTicketStatus.Testing.ToString()).Id;
			int statusResolved = context.TicketStatuses.FirstOrDefault(p => p.Name == BTTicketStatus.Resolved.ToString()).Id;


			try
			{
				IList<Ticket> tickets = new List<Ticket>() {
                                //PORTFOLIO
                                new Ticket() {Title = "Portfolio Ticket 1", Description = "Ticket details for portfolio ticket 1", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = portfolioId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								new Ticket() {Title = "Portfolio Ticket 2", Description = "Ticket details for portfolio ticket 2", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = portfolioId, TicketPriorityId = priorityMedium, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
								new Ticket() {Title = "Portfolio Ticket 3", Description = "Ticket details for portfolio ticket 3", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = portfolioId, TicketPriorityId = priorityHigh, TicketStatusId = statusDev, TicketTypeId = typeEnhancement},
								new Ticket() {Title = "Portfolio Ticket 4", Description = "Ticket details for portfolio ticket 4", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = portfolioId, TicketPriorityId = priorityUrgent, TicketStatusId = statusTest, TicketTypeId = typeDefect},
								new Ticket() {Title = "Portfolio Ticket 5", Description = "Ticket details for portfolio ticket 5", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = portfolioId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								new Ticket() {Title = "Portfolio Ticket 6", Description = "Ticket details for portfolio ticket 6", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = portfolioId, TicketPriorityId = priorityMedium, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
								new Ticket() {Title = "Portfolio Ticket 7", Description = "Ticket details for portfolio ticket 7", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = portfolioId, TicketPriorityId = priorityHigh, TicketStatusId = statusDev, TicketTypeId = typeEnhancement},
								new Ticket() {Title = "Portfolio Ticket 8", Description = "Ticket details for portfolio ticket 8", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = portfolioId, TicketPriorityId = priorityUrgent, TicketStatusId = statusTest, TicketTypeId = typeDefect},
                                //BLOG
                                new Ticket() {Title = "Blog Ticket 1", Description = "Ticket details for blog ticket 1", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = blogId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeDefect},
								new Ticket() {Title = "Blog Ticket 2", Description = "Ticket details for blog ticket 2", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = blogId, TicketPriorityId = priorityMedium, TicketStatusId = statusDev, TicketTypeId = typeEnhancement},
								new Ticket() {Title = "Blog Ticket 3", Description = "Ticket details for blog ticket 3", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = blogId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
								new Ticket() {Title = "Blog Ticket 4", Description = "Ticket details for blog ticket 4", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = blogId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								new Ticket() {Title = "Blog Ticket 5", Description = "Ticket details for blog ticket 5", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = blogId, TicketPriorityId = priorityLow, TicketStatusId = statusDev,  TicketTypeId = typeDefect},
								
								
                                //BUGTRACKER                                                                                                                         
                                new Ticket() {Title = "Bug Tracker Ticket 1", Description = "Ticket details for bug tracker ticket 1", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								new Ticket() {Title = "Bug Tracker Ticket 2", Description = "Ticket details for bug tracker ticket 2", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								new Ticket() {Title = "Bug Tracker Ticket 3", Description = "Ticket details for bug tracker ticket 3", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								new Ticket() {Title = "Bug Tracker Ticket 4", Description = "Ticket details for bug tracker ticket 4", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								new Ticket() {Title = "Bug Tracker Ticket 5", Description = "Ticket details for bug tracker ticket 5", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								
								
                                //MOVIE
                                new Ticket() {Title = "Movie Ticket 1", Description = "Ticket details for movie ticket 1", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = movieId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeDefect},
								new Ticket() {Title = "Movie Ticket 2", Description = "Ticket details for movie ticket 2", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = movieId, TicketPriorityId = priorityMedium, TicketStatusId = statusDev, TicketTypeId = typeEnhancement},
								new Ticket() {Title = "Movie Ticket 3", Description = "Ticket details for movie ticket 3", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = movieId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
								new Ticket() {Title = "Movie Ticket 4", Description = "Ticket details for movie ticket 4", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = movieId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
								new Ticket() {Title = "Movie Ticket 5", Description = "Ticket details for movie ticket 5", Created = DateTimeOffset.Now.ToUniversalTime(), ProjectId = movieId, TicketPriorityId = priorityLow, TicketStatusId = statusDev,  TicketTypeId = typeDefect},																
				};


				var dbTickets = context.Tickets.Select(c => c.Title).ToList();
				await context.Tickets.AddRangeAsync(tickets.Where(c => !dbTickets.Contains(c.Title)));
				await context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine("*************  ERROR  *************");
				Console.WriteLine("Error Seeding Tickets.");
				Console.WriteLine(ex.Message);
				Console.WriteLine("***********************************");
				throw;
			}
		}
	}
}
