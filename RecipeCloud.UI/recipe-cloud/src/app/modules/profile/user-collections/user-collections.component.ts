import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Recipe } from '../../../core/models/recipe.model';
import { Router } from '@angular/router';
import { RecipeService } from '../../../core/services/recipe.service';
import { AuthService } from '../../../core/services/auth.service';
import { Collection } from '../../../core/models/collection.model';
import { CollectionService } from '../../../core/services/collection.service';


@Component({
  selector: 'app-user-collections',
  templateUrl: './user-collections.component.html',
  styleUrls: ['./user-collections.component.css']
})
export class UserCollectionsComponent {
  collections: Collection[] = [];
  userId: string | undefined;
  userCollections: Collection[] = [];
  
  // @Output() recipeCreated = new EventEmitter<Recipe>();
  // @Output() recipeUpdated = new EventEmitter<Recipe>();
  // @Output() recipeDeleted = new EventEmitter<string>();

  constructor(private router: Router, private collectionService: CollectionService, private authService: AuthService) {
    this.userId = this.authService.getCurrentUserId();
    console.log(this.userId);

    if (this.userId) {
      this.collectionService.getByUserId(this.userId).subscribe((collections) => {
        this.collections = collections;
        this.collections.forEach(collection => {
          console.log(collection.title);
        });
      });
    }
  }

  editCollection(collectionId: string): void {
    // const collection = this.userCollections.find(c => c.id === collectionId);
    // if (collection) {
    //   const newName = prompt('Введіть нову назву колекції:', collection.name);
    //   if (newName && newName.trim()) {
    //     collection.name = newName.trim();
    //     console.log('Collection updated:', collection);
    //   }
    // }
  }

  deleteCollection(collectionId: string): void {
    if (confirm('Ви впевнені, що хочете видалити цю колекцію?')) {
      // this.userCollections = this.userCollections.filter(c => c.id !== collectionId);
      console.log('Collection deleted:', collectionId);
    }
  }

  addRecipeToCollection(collectionId: string): void {
    // This would typically open a modal with available recipes to add
    console.log('Adding recipe to collection:', collectionId);
    // For demo purposes, let's add a random recipe
    // const collection = this.userCollections.find(c => c.id === collectionId);
    // if (collection && this.userRecipes.length > 0) {
    //   const recipeToAdd = this.userRecipes[0]; // Just add the first recipe for demo
    //   if (!collection.recipes.find(r => r.id === recipeToAdd.id)) {
    //     collection.recipes.push(recipeToAdd);
    //     console.log('Recipe added to collection');
    //   }
    // }
  }

  removeFromCollection(collectionId: string, recipeId: string): void {
    // const collection = this.userCollections.find(c => c.id === collectionId);
    // if (collection) {
    //   collection.recipes = collection.recipes.filter(r => r.id !== recipeId);
    //   console.log('Recipe removed from collection');
    // }
  }

  // Collection management methods
  createNewCollection(): void {
    const name = prompt('Введіть назву нової колекції:');
    if (name && name.trim()) {
      const newCollection: Collection = {
        id: Date.now().toString(),
        title: name.trim(),
        recipes: [],
        createdAt: new Date(),
        createdBy: '',
        updatedAt: new Date(),
        totalCalories: 0,
        totalProtein: 0,
        totalFat: 0,
        totalCarbohydrates: 0
      };
      // this.userCollections.push(newCollection);
      console.log('New collection created:', newCollection);
    }
  }
}