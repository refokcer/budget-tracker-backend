using System.Text.Json.Serialization;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Models;

public class Category : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public TransactionCategoryType Type { get; set; }
    public string? Description { get; set; }
    public string UserId { get; set; } = null!;
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
}
