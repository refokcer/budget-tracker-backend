namespace budget_tracker_backend.Services.AdminData;

using budget_tracker_backend.Dto.AdminData;

public interface IAdminDataManager
{
    Task ClearCurrentUserDataAsync(CancellationToken cancellationToken);
    Task<AdminDataImportResultDto> ImportAsync(AdminDataImportDto dto, CancellationToken cancellationToken);
    IReadOnlyList<AdminDataTemplateDto> GetTemplates();
    Task<AdminDataImportDto> GetTemplateAsync(string templateId, CancellationToken cancellationToken);
    AdminDataImportDto BuildSample();
}
