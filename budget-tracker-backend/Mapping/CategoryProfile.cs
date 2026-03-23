using AutoMapper;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();

        CreateMap<CreateCategoryDto, Category>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());

        CreateMap<CategoryDto, Category>()
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());
    }
}
