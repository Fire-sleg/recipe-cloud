import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, FormControl } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { UserProfile } from '../../../core/models/user-profile.model';
import { UserPreferences } from '../../../core/models/user-preferences.model';
import { Collection } from '../../../core/models/collection.model';
import { Recipe } from '../../../core/models/recipe.model';


@Component({
  selector: 'app-user-profile',
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {
  userRecipes: Recipe[] = [];
  userCollections: Collection[] = [];
  profileForm!: FormGroup;
  isSubmitting = false;
  activeTab: 'preferences' | 'recipes' | 'collections' = 'preferences';

  // Available options for form
  availableDiets: string[] = [
    // 'Вегетаріанська',
    // 'Веганська', 
    // 'Безглютенова',
    // 'Кето',
    // 'Палео',
    // 'Середземноморська',
    // 'Дієта DASH',
    // 'Низьковуглеводна'
    "Вегетаріанський",
    "Без глютену",
    "Без цукру",
    "Кето",
    "Здоровий",
    "Веганський",
    "Невегетаріанський"
  ];

  availableAllergens: string[] = [
    'Молочні продукти',
    'Яйця',
    'Риба',
    'Морепродукти',
    'Горіхи дерев',
    'Арахіс',
    'Глютен',
    'Соя',
    'Кунжут'
  ];

  availableCuisines: string[] = [
    // 'Українська',
    // 'Італійська',
    // 'Французька',
    // 'Японська',
    // 'Китайська',
    // 'Індійська',
    // 'Мексиканська',
    // 'Грецька',
    // 'Тайська',
    // 'Американська',
    // 'Німецька',
    // 'Іспанська'
    "Латиноамериканська",
    "Європейська",
    "Міжнародна",
    "Грецька",
    "Азійська",
    "Українська"
  ];

  constructor(private fb: FormBuilder, private authService: AuthService) {}

  ngOnInit(): void {
    this.initializeForm();
    this.loadUserProfile();
    this.loadUserData();
  }

  // Tab navigation methods
  setActiveTab(tab: 'preferences' | 'recipes' | 'collections'): void {
    this.activeTab = tab;
  }

  private initializeForm(): void {
    this.profileForm = this.fb.group({
      diets: this.createCheckboxArray(this.availableDiets),
      allergens: this.createCheckboxArray(this.availableAllergens),
      cuisines: this.createCheckboxArray(this.availableCuisines)
    });
  }

  private createCheckboxArray(options: string[]): FormArray {
    const controls = options.map(() => new FormControl(false));
    return this.fb.array(controls);
  }

  // Getters for easy access to FormArrays
  get diets(): FormArray {
    return this.profileForm.get('diets') as FormArray;
  }

  get allergens(): FormArray {
    return this.profileForm.get('allergens') as FormArray;
  }

  get cuisines(): FormArray {
    return this.profileForm.get('cuisines') as FormArray;
  }

  private loadUserProfile(): void {
    const preferences = this.getUserPreferencesFromStorage();
    if (preferences) {
      this.populateForm(preferences);
    }
  }

  private loadUserData(): void {
    // Load user recipes and collections from API or localStorage
    // This is where you would make API calls to get user's recipes and collections
    console.log('Loading user recipes and collections...');
  }

  private getUserPreferencesFromStorage(): any {
    try {
      const preferences = localStorage.getItem('preferences');
      return preferences ? JSON.parse(preferences) : null;
    } catch (error) {
      console.error('Помилка завантаження профілю:', error);
      return null;
    }
  }

  private populateForm(profile: any): void {
    if (profile.diets) {
      this.setCheckboxArrayValues(this.diets, profile.diets);
    }
    
    if (profile.allergens) {
      this.setCheckboxArrayValues(this.allergens, profile.allergens);
    }
    
    if (profile.cuisines) {
      this.setCheckboxArrayValues(this.cuisines, profile.cuisines);
    }
  }

  private setCheckboxArrayValues(formArray: FormArray, selectedItems: string[]): void {
    formArray.controls.forEach((control, index) => {
      const optionName = this.getOptionNameByIndex(formArray, index);
      control.setValue(selectedItems.includes(optionName));
    });
  }

  private getOptionNameByIndex(formArray: FormArray, index: number): string {
    if (formArray === this.diets) {
      return this.availableDiets[index];
    } else if (formArray === this.allergens) {
      return this.availableAllergens[index];
    } else if (formArray === this.cuisines) {
      return this.availableCuisines[index];
    }
    return '';
  }

  onSubmit(): void {
    if (this.profileForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;
      
      const formData = this.getFormData();
      console.log('Sending preferences:', formData);
      
      this.authService.savePreferences(formData).subscribe({
        next: () => {
          console.log('Preferences saved');
          this.isSubmitting = false;
          this.showSuccessMessage();
        },
        error: err => {
          console.error('Error saving prefs:', err);
          this.isSubmitting = false;
        }
      });
      
      localStorage.setItem('preferences', JSON.stringify(formData));
    }
  }

  private getFormData(): any {
    const formValue = this.profileForm.value;
    const userId = this.authService.getCurrentUserId();
    const prefs: UserPreferences = {
      userId: userId,
      dietaryPreferences: this.getSelectedOptions(formValue.diets, this.availableDiets),
      allergens: this.getSelectedOptions(formValue.allergens, this.availableAllergens),
      favoriteCuisines: this.getSelectedOptions(formValue.cuisines, this.availableCuisines)
    };

    return prefs;
  }

  private getSelectedOptions(selections: boolean[], options: string[]): string[] {
    return options.filter((_, index) => selections[index]);
  }

  private showSuccessMessage(): void {
    alert('Профіль успішно збережено!');
  }

  resetForm(): void {
    if (!this.isSubmitting) {
      this.profileForm.reset();
      
      this.diets.controls.forEach(control => control.setValue(false));
      this.allergens.controls.forEach(control => control.setValue(false));
      this.cuisines.controls.forEach(control => control.setValue(false));
      
      console.log('Форму скинуто');
    }
  }

  // Recipe management methods
  createNewRecipe(): void {
    // Navigate to recipe creation page or open modal
    console.log('Creating new recipe...');
    // this.router.navigate(['/recipes/create']);
  }

  editRecipe(recipeId: string): void {
    console.log('Editing recipe:', recipeId);
    // this.router.navigate(['/recipes/edit', recipeId]);
  }

  deleteRecipe(recipeId: string): void {
    if (confirm('Ви впевнені, що хочете видалити цей рецепт?')) {
      // this.userRecipes = this.userRecipes.filter(recipe => recipe.id !== recipeId);
      console.log('Recipe deleted:', recipeId);
      // API call to delete recipe
    }
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

  // Utility methods for displaying selected preferences
  getSelectedDiets(): string[] {
    return this.getSelectedOptions(this.diets.value, this.availableDiets);
  }

  getSelectedAllergens(): string[] {
    return this.getSelectedOptions(this.allergens.value, this.availableAllergens);
  }

  getSelectedCuisines(): string[] {
    return this.getSelectedOptions(this.cuisines.value, this.availableCuisines);
  }

  hasSelectedDiets(): boolean {
    return this.diets.value.some((selected: boolean) => selected);
  }

  hasSelectedAllergens(): boolean {
    return this.allergens.value.some((selected: boolean) => selected);
  }

  hasSelectedCuisines(): boolean {
    return this.cuisines.value.some((selected: boolean) => selected);
  }
}
