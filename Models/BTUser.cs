using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTAnshDesai.Models
{
	public class BTUser : IdentityUser
	{
		[Required]
		[Display(Name = "First Name")]
		public string? FirstName { get; set; }

		[Required]
		[Display(Name = "Sur Name")]
		public string? SurName { get; set; }

		[NotMapped]
		[Display(Name = "Full Name")]
		public string? FullName { get { return $"{FirstName} {SurName}"; } }

		[NotMapped]
		[DataType(DataType.Upload)]
		public IFormFile? AvatarFormFile { get; set; }

		[DisplayName("Avatar")]
		public string? AvatarFileName { get; set; }
		public byte[]? AvatarFileData { get; set; }

		[DisplayName("File Extension")]
		public string? ImageFileContentType { get; set; }

		public int CompanyId { get; set; }

		public virtual Company? Company { get; set; }
		public virtual ICollection<Project>? Projects { get; set; }

	}
}
