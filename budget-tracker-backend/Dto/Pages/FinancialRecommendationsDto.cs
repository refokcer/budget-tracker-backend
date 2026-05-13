namespace budget_tracker_backend.Dto.Pages;

public class FinancialRecommendationsDto
{
    public DateTime GeneratedAt { get; set; }
    public string OverallStatus { get; set; } = null!;
    public string Headline { get; set; } = null!;
    public string Explanation { get; set; } = null!;
    public int FinancialStabilityIndex { get; set; }
    public int PreviousFinancialStabilityIndex { get; set; }
    public int FinancialStabilityDelta { get; set; }
    public string FinancialStabilityLevel { get; set; } = null!;
    public int BehavioralScore { get; set; }
    public int PreviousBehavioralScore { get; set; }
    public int BehavioralScoreDelta { get; set; }
    public string BehavioralLevel { get; set; } = null!;
    public List<FinancialRecommendationSignalDto> Signals { get; set; } = new();
    public List<FinancialRecommendationActionDto> PriorityActions { get; set; } = new();
    public List<FinancialRecommendationSectionDto> Sections { get; set; } = new();
}

public class FinancialRecommendationSignalDto
{
    public string Title { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? PreviousValue { get; set; }
    public string Level { get; set; } = null!;
    public string Explanation { get; set; } = null!;
}

public class FinancialRecommendationActionDto
{
    public string Title { get; set; } = null!;
    public string Why { get; set; } = null!;
    public string WhatToDo { get; set; } = null!;
    public string Impact { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string? ActionLink { get; set; }
}

public class FinancialRecommendationSectionDto
{
    public string Title { get; set; } = null!;
    public string Summary { get; set; } = null!;
    public string Status { get; set; } = null!;
    public List<FinancialRecommendationActionDto> Actions { get; set; } = new();
}
