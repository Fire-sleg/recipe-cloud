using AutoMapper;
using RecipeService.Models.Categories.DTOs;
using RecipeService.Models.Categories;
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

            CreateMap<Recipe, RecipeDTO>().ReverseMap(); 
            CreateMap<Recipe, RecipeCreateDTO>().ReverseMap();
            CreateMap<Recipe, RecipeUpdateDTO>().ReverseMap();

            CreateMap<Collection, CollectionDTO>().ReverseMap();
            CreateMap<Collection, CollectionCreateDTO>().ReverseMap();
            CreateMap<Collection, CollectionUpdateDTO>().ReverseMap();


            CreateMap<Category, CategoryDTO>().ReverseMap();
            CreateMap<Category, CategoryCreateDTO>().ReverseMap();
            CreateMap<Category, CategoryUpdateDTO>().ReverseMap();

        }

    }
}
