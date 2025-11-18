using AutoMapper;
using UniShareProject.Repository.Models;
using UniShareProject.services.Models;
using UniShareProject.services.DTOs;
using UniShareProject.Repository.Repositories;

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

        CreateMap<User, AdminUserDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserID))
            .ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.ProfileImageURL));

        // Item mappings
        CreateMap<Item, ItemDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ItemID))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryID))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => GetCategoryName(src.CategoryID)))
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

        // Messaging mappings - Entity to DTO mapping with proper DB column mapping
        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.MessageId, opt => opt.MapFrom(src => src.MessageID))
            .ForMember(dest => dest.ConversationId, opt => opt.MapFrom(src => src.ConversationID))
            .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.SenderID))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp));

        // Paged messaging mappings
        CreateMap<PagedResult<Message>, PagedResult<MessageDto>>()
            .ForMember(d => d.Items, m => m.MapFrom(s => s.Items))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total))
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
            .ForMember(dest => dest.TotalPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.HasNextPage, opt => opt.MapFrom(src => src.HasNextPage))
            .ForMember(dest => dest.HasPreviousPage, opt => opt.MapFrom(src => src.HasPreviousPage));

        // Conversation mappings
        CreateMap<ConversationListData, ConversationListItem>()
            .ForMember(dest => dest.ConversationId, opt => opt.MapFrom(src => src.ConversationID))
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemID))
            .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.LastUpdated))
            .ForMember(dest => dest.LastMessage, opt => opt.MapFrom(src => src.LastMessage))
            .ForMember(dest => dest.OtherUserId, opt => opt.MapFrom(src => src.OtherUserId))
            .ForMember(dest => dest.OtherUserName, opt => opt.MapFrom(src => src.OtherUserName))
            .ForMember(dest => dest.UnreadCount, opt => opt.MapFrom(src => src.UnreadCount));

        // Paged conversation mappings  
        CreateMap<PagedResult<ConversationListData>, PagedResult<ConversationListItem>>()
            .ForMember(d => d.Items, m => m.MapFrom(s => s.Items))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total))
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
            .ForMember(dest => dest.TotalPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.HasNextPage, opt => opt.MapFrom(src => src.HasNextPage))
            .ForMember(dest => dest.HasPreviousPage, opt => opt.MapFrom(src => src.HasPreviousPage));
    }

    /// <summary>
    /// Maps category ID to category name
    /// </summary>
    /// <param name="categoryId">Category ID (1=Electronics, 2=Books, 3=Clothing, 4=Furniture, 5=Sports and Recreation, 6=Other)</param>
    /// <returns>Category name string</returns>
    private static string? GetCategoryName(int? categoryId)
    {
        return categoryId switch
        {
            1 => "Electronics",
            2 => "Books", 
            3 => "Clothing",
            4 => "Furniture",
            5 => "Sports & Recreation",
            6 => "Other",
            _ => null
        };
    }
}