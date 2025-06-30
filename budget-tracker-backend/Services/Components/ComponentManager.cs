namespace budget_tracker_backend.Services.Components;

using AutoMapper;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Services.Currencies;

public class ComponentManager : IComponentManager
{
    private readonly IAccountManager _accountManager;
    private readonly ICategoryManager _categoryManager;
    private readonly ICurrencyManager _currencyManager;
    private readonly IMapper _mapper;

    public ComponentManager(
        IAccountManager accountManager,
        ICategoryManager categoryManager,
        ICurrencyManager currencyManager,
        IMapper mapper)
    {
        _accountManager = accountManager;
        _categoryManager = categoryManager;
        _currencyManager = currencyManager;
        _mapper = mapper;
    }

    public async Task<IncomeModalDto> GetIncomeModalAsync(CancellationToken ct)
    {
        var currencies = await _currencyManager.GetAllAsync(ct);
        var categories = await _categoryManager.GetByTypeAsync(TransactionCategoryType.Income, ct);
        var accounts = await _accountManager.GetAllAsync(ct);

        return new IncomeModalDto
        {
            Currencies = _mapper.Map<List<CurrencyDto>>(currencies),
            Categories = _mapper.Map<List<CategoryDto>>(categories),
            Accounts = _mapper.Map<List<AccountDto>>(accounts)
        };
    }

    public async Task<ExpenseModalDto> GetExpenseModalAsync(CancellationToken ct)
    {
        var currencies = await _currencyManager.GetAllAsync(ct);
        var categories = await _categoryManager.GetByTypeAsync(TransactionCategoryType.Expense, ct);
        var accounts = await _accountManager.GetAllAsync(ct);

        return new ExpenseModalDto
        {
            Currencies = _mapper.Map<List<CurrencyDto>>(currencies),
            Categories = _mapper.Map<List<CategoryDto>>(categories),
            Accounts = _mapper.Map<List<AccountDto>>(accounts)
        };
    }

    public async Task<TransferModalDto> GetTransferModalAsync(CancellationToken ct)
    {
        var currencies = await _currencyManager.GetAllAsync(ct);
        var accounts = await _accountManager.GetAllAsync(ct);
        var categories = await _categoryManager.GetByTypeAsync(TransactionCategoryType.Transaction, ct);

        return new TransferModalDto
        {
            Currencies = _mapper.Map<List<CurrencyDto>>(currencies),
            Accounts = _mapper.Map<List<AccountDto>>(accounts),
            Categories = _mapper.Map<List<CategoryDto>>(categories)
        };
    }

    public async Task<EditPlanModalDto> GetEditPlanModalAsync(CancellationToken ct)
    {
        var currencies = await _currencyManager.GetAllAsync(ct);
        var categories = await _categoryManager.GetByTypeAsync(TransactionCategoryType.Expense, ct);

        return new EditPlanModalDto
        {
            Currencies = _mapper.Map<List<CurrencyDto>>(currencies),
            Categories = _mapper.Map<List<CategoryDto>>(categories)
        };
    }

    public async Task<ManageAccountsDto> GetManageAccountsAsync(CancellationToken ct)
    {
        var accounts = await _accountManager.GetAllAsync(ct);
        return new ManageAccountsDto
        {
            Accounts = _mapper.Map<List<AccountDto>>(accounts)
        };
    }

    public async Task<ManageCategoriesDto> GetManageCategoriesAsync(TransactionCategoryType type, CancellationToken ct)
    {
        var categories = await _categoryManager.GetByTypeAsync(type, ct);
        return new ManageCategoriesDto
        {
            Categories = _mapper.Map<List<CategoryDto>>(categories)
        };
    }
}
