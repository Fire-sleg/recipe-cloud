using AutoMapper;
using RecipeService.Models.Collections;
using RecipeService.Models.Collections.DTOs;
using RecipeService.Models.Recipes;
using RecipeService.Models.Recipes.DTOs;

namespace RecipeService
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            //CreateMap<Recipe, RecipeDTO>();
            //CreateMap<RecipeCreateDTO, Recipe>()
            //    .ForMember(dest => dest.IsPremium, opt => opt.MapFrom(src => false))
            //    .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            //CreateMap<RecipeUpdateDTO, Recipe>()
            //    .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null && (srcMember is not int || (int)srcMember != 0)));


            CreateMap<Recipe, RecipeDTO>().ReverseMap(); ;

            CreateMap<Recipe, RecipeCreateDTO>().ReverseMap();
            CreateMap<Recipe, RecipeUpdateDTO>().ReverseMap();

            CreateMap<Collection, CollectionDTO>().ReverseMap();
            CreateMap<Collection, CollectionCreateDTO>().ReverseMap();
            CreateMap<Collection, CollectionUpdateDTO>().ReverseMap();


            /*
             
             CreateMap<Recipe, RecipeDTO>().ReverseMap();
            CreateMap<Recipe, RecipeCreateDTO>().ReverseMap()
                .ForMember(dest => dest.IsPremium, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());
            CreateMap<Recipe, RecipeUpdateDTO>().ReverseMap()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null && (srcMember is not int || (int)srcMember != 0)));
             
             */
        }

    }
}
