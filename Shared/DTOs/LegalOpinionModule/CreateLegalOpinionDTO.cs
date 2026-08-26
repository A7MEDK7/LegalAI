using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

public class CreateLegalOpinionDTO {
    [Required(ErrorMessage = "Title Is Required!")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Question Is Required!")]
    public string Question { get; set; } = null!;

    public string? Answer { get; set; }

    public IFormFile? File { get; set; } = null!;
}