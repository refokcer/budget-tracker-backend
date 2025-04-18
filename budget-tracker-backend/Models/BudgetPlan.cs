﻿using budget_tracker_backend.Models.Enums;
using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class BudgetPlan
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public BudgetPlanType Type { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    [JsonIgnore]
    public virtual ICollection<BudgetPlanItem>? Items { get; set; }
}
