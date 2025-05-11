import { Component, OnInit } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { RecipeService } from '../../../core/services/recipe.service';
import { Recipe } from '../../../core/models/recipe.model';
import { User } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-recipe-list',
  templateUrl: './recipe-list.component.html',
  styleUrls: ['./recipe-list.component.css']
})
export class RecipeListComponent implements OnInit {
  recipes$!: Observable<Recipe[]>;
  isAuthenticated$: Observable<boolean> | undefined;
  currentUser$: Observable<User | null> | undefined;

  constructor(private recipeService: RecipeService, private authService: AuthService) {}

  ngOnInit(): void {
    this.isAuthenticated$ = this.authService.currentUser$.pipe(map(user => !!user));
    this.currentUser$ = this.authService.currentUser$;
    const recipesList: Recipe[] = [
      {
        id: '1',
        title: 'Овочеве рагу',
        description: 'Ситне овочеве рагу з картоплею, морквою, кабачками та томатами.',
        ingredients: ['картопля', 'морква', 'кабачок', 'цибуля', 'помідори', 'сіль', 'олія'],
        cookingTime: 40,
        difficulty: 'easy',
        imageUrl: 'https://example.com/vegetable-stew.jpg',
        createdBy: 'user123',
        createdByUsername: 'natasha_cook',
        createdAt: '2024-10-01T12:00:00Z',
        updatedAt: '2024-10-01T12:00:00Z',
        diets: ['vegetarian', 'vegan'],
        allergens: [],
        cuisine: 'українська',
        tags: ['рагу', 'овочі', 'здорове'],
        calories: 150,
        protein: 3,
        fat: 5,
        carbohydrates: 25,
        isPremium: false,
      },
      {
        id: '2',
        title: 'Борщ український',
        description: 'Традиційний борщ з буряком, капустою та м’ясом.',
        ingredients: ['буряк', 'картопля', 'морква', 'капуста', 'свинина', 'цибуля', 'томатна паста', 'часник'],
        cookingTime: 90,
        difficulty: 'medium',
        imageUrl: 'https://example.com/borscht.jpg',
        createdBy: 'user234',
        createdByUsername: 'ivan_chef',
        createdAt: '2024-10-02T15:00:00Z',
        updatedAt: '2024-10-03T09:00:00Z',
        diets: [],
        allergens: [],
        cuisine: 'українська',
        tags: ['борщ', 'суп', 'традиційне'],
        calories: 220,
        protein: 10,
        fat: 8,
        carbohydrates: 25,
        isPremium: true,
      },
      {
        id: '3',
        title: 'Паста з лососем у вершковому соусі',
        description: 'Італійська паста з лососем та вершковим соусом.',
        ingredients: ['паста', 'лосось', 'вершки', 'часник', 'пармезан', 'оливкова олія'],
        cookingTime: 30,
        difficulty: 'medium',
        imageUrl: 'https://example.com/salmon-pasta.jpg',
        createdBy: 'user345',
        createdByUsername: 'maria_italianfood',
        createdAt: '2024-10-05T18:00:00Z',
        updatedAt: '2024-10-05T18:00:00Z',
        diets: ['pescatarian'],
        allergens: ['fish', 'dairy', 'gluten'],
        cuisine: 'італійська',
        tags: ['паста', 'лосось', 'вечеря'],
        calories: 450,
        protein: 20,
        fat: 18,
        carbohydrates: 50,
        isPremium: true,
      },
      {
        id: '4',
        title: 'Салат "Цезар"',
        description: 'Класичний салат "Цезар" з куркою, сухариками та соусом.',
        ingredients: ['куряче філе', 'салат ромен', 'пармезан', 'сухарики', 'соус цезар', 'оливкова олія'],
        cookingTime: 20,
        difficulty: 'easy',
        imageUrl: 'https://example.com/caesar-salad.jpg',
        createdBy: 'user456',
        createdByUsername: 'saladqueen',
        createdAt: '2024-10-06T10:00:00Z',
        updatedAt: '2024-10-06T10:00:00Z',
        diets: [],
        allergens: ['egg', 'dairy', 'gluten'],
        cuisine: 'американська',
        tags: ['салат', 'курка', 'легке'],
        calories: 300,
        protein: 25,
        fat: 15,
        carbohydrates: 12,
        isPremium: false,
      },
      {
        id: '5',
        title: 'Сирники',
        description: 'Пухкі домашні сирники з родзинками.',
        ingredients: ['творог', 'яйця', 'цукор', 'борошно', 'родзинки', 'сіль', 'олія'],
        cookingTime: 25,
        difficulty: 'easy',
        imageUrl: 'https://example.com/syrnyky.jpg',
        createdBy: 'user567',
        createdByUsername: 'sweetlena',
        createdAt: '2024-10-07T08:30:00Z',
        updatedAt: '2024-10-07T08:30:00Z',
        diets: ['vegetarian'],
        allergens: ['dairy', 'egg', 'gluten'],
        cuisine: 'українська',
        tags: ['десерт', 'сніданок', 'солодке'],
        calories: 280,
        protein: 12,
        fat: 14,
        carbohydrates: 25,
        isPremium: false,
      }
    ];
    
    this.recipes$ = of(recipesList);//this.recipeService.getRecipes();
    
  }
}