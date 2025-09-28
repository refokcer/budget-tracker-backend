using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace budget_tracker_backend.Dto.ChatGpt;

public sealed class ParseTransactionsRequest
{
    [JsonPropertyName("pdf")]
    [FromForm(Name = "pdf")]
    [Required]
    public IFormFile? Pdf { get; set; }
}
