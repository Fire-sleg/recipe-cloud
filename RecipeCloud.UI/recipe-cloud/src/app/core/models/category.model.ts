import { BreadcrumbItem } from "./breadcrumb.model";
import { Recipe } from "./recipe.model";

export interface Category {
    id: string;
    name: string;
    transliteratedName: string;
    imageUrl: string;
    breadcrumbPath: BreadcrumbItem[] | null;
    parentCategoryId: string | null;
    subCategories: Category[] | null;
    recipes: Recipe[] | null;
}