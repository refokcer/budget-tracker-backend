using AutoMapper;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        // Из сущности → DTO
        CreateMap<Category, CategoryDto>();

        // Из DTO создания → сущность
        CreateMap<CreateCategoryDto, Category>();

        // Из полного DTO → сущность
        CreateMap<CategoryDto, Category>();
    }
}
