using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace budget_tracker_backend.Dto.Pdf;

public class ParsePdfRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;
}
