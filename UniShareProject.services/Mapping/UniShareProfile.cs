using AutoMapper;
using UniShareProject.Repository.Models;
using UniShareProject.services.Models;
using UniShareProject.services.DTOs;

namespace UniShareProject.services.Mapping;

/// <summary>
/// AutoMapper profile for UniShare project mappings
/// </summary>
public class UniShareProfile : Profile
{
    public UniShareProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserID))
            .ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.ProfileImageURL));

        // Item mappings
        CreateMap<Item, ItemDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ItemID))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryID))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.ConditionId, opt => opt.MapFrom(src => src.ConditionID))
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.StatusID))
            .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.SellerID));

        CreateMap<CreateItemRequest, Item>()
            .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.ConditionID, opt => opt.MapFrom(src => src.ConditionId))
            .ForMember(dest => dest.StatusID, opt => opt.MapFrom(src => (byte)1)) // Active status
            .ForMember(dest => dest.PostedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ItemID, opt => opt.Ignore())
            .ForMember(dest => dest.SellerID, opt => opt.Ignore()); // Set by service

        // PagedResult mappings
        CreateMap<PagedResult<Item>, PagedResultDto<ItemDto>>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.TotalPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.HasNextPage, opt => opt.MapFrom(src => src.HasNextPage))
            .ForMember(dest => dest.HasPreviousPage, opt => opt.MapFrom(src => src.HasPreviousPage));
    }
}