export interface BreadcrumbItem{
    id: string;
    name: string;
    transliteratedName: string;
    order: number;
    categoryId: string | null | undefined;
    recipeId: string | null | undefined;
}