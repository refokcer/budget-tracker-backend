namespace budget_tracker_backend.Services.AdminData;

using budget_tracker_backend.Dto.AdminData;

public interface IAdminDataTemplateProvider
{
    IReadOnlyList<AdminDataTemplateDto> GetTemplates();
    Task<AdminDataImportDto> GetTemplateAsync(string templateId, CancellationToken cancellationToken);
}
