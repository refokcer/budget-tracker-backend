using AutoMapper;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();

        CreateMap<CreateCategoryDto, Category>();

        CreateMap<CategoryDto, Category>();
    }
}
