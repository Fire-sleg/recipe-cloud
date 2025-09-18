import { Component, OnInit, Output, EventEmitter, Input, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { RecipeService } from '../../../core/services/recipe.service';
import { AuthService } from '../../../core/services/auth.service';
import { Recipe } from '../../../core/models/recipe.model';
import { Category } from '../../../core/models/category.model';
import { ActivatedRoute, Router } from '@angular/router';
import { CategoryService } from '../../../core/services/category.service';
import { APIResponse } from '../../../core/models/api-response';

@Component({
  selector: 'app-recipe-form',
  templateUrl: './recipe-form.component.html',
  styleUrls: ['./recipe-form.component.css']
})
export class RecipeFormComponent implements OnInit, OnChanges {
  @Input() category: Category | null = null;
  @Input() recipeToEdit: Recipe | null = null;
  
  @Output() recipeCreated = new EventEmitter<Recipe>();
  @Output() recipeUpdated = new EventEmitter<Recipe>();
  @Output() formClosed = new EventEmitter<void>();

  categories: Category[] | null = null;
  recipeForm: FormGroup;
  isSubmitting = false;
  showForm = false;
  isEditMode = false;
  
  // File upload properties
  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  fileError: string = '';
  maxFileSize = 5 * 1024 * 1024; // 5MB
  allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];

  constructor(
    private fb: FormBuilder,
    private recipeService: RecipeService,
    private categoryService: CategoryService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.recipeForm = this.createForm();
    // Always fetch categories to populate dropdown
    this.categoryService.getSubCategoriesWithRecipes().subscribe((response: Category[]) => {
      this.categories = response;
      // Set categoryId if provided via Input
      if (this.category?.id) {
        this.recipeForm.patchValue({ categoryId: this.category.id });
      }
    });
  }

  ngOnInit(): void {
    // Component initialization
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['recipeToEdit'] && this.recipeToEdit) {
      this.isEditMode = true;
      this.showForm = true;
      this.populateForm(this.recipeToEdit);
    }
    if (changes['category'] && this.category?.id) {
      this.recipeForm.patchValue({ categoryId: this.category.id });
    }
  }

  getCategoryDisplayName(): string {
    if (this.recipeForm.value.categoryId && this.categories) {
      const foundCategory = this.categories.find(c => c.id === this.recipeForm.value.categoryId);
      return foundCategory?.name || 'Не вибрано';
    }
    return 'Не вибрано';
  }

  createForm(): FormGroup {
    return this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      categoryId: ['', Validators.required], // Initialize empty to require selection
      cookingTime: [0, [Validators.required, Validators.min(1)]],
      servings: [1, [Validators.required, Validators.min(1)]],
      difficulty: ['Easy', Validators.required],
      cuisine: ['', [Validators.required, Validators.minLength(3)]],
      
      // Nutritional Information
      calories: [0, [Validators.min(0)]],
      protein: [0, [Validators.min(0)]],
      carbohydrates: [0, [Validators.min(0)]],
      fat: [0, [Validators.min(0)]],
      
      // Characteristics as FormArrays
      allergens: this.fb.array([this.createCharacteristicGroup()]),
      diets: this.fb.array([this.createCharacteristicGroup()]),
      tags: this.fb.array([this.createCharacteristicGroup()]),
      
      // Ingredients and Instructions
      ingredients: this.fb.array([this.createIngredientGroup()]),
      instructions: this.fb.array([this.createInstructionGroup()])
    });
  }

  createCharacteristicGroup(): FormGroup {
    return this.fb.group({
      name: ['']
    });
  }

  createIngredientGroup(): FormGroup {
    return this.fb.group({
      name: ['', Validators.required],
      amount: ['', Validators.required],
      unit: ['', Validators.required]
    });
  }

  createInstructionGroup(): FormGroup {
    return this.fb.group({
      step: ['', Validators.required],
      order: [1, Validators.required]
    });
  }

  // Populate form with recipe data for editing
  private populateForm(recipe: Recipe): void {
    this.recipeForm.patchValue({
      title: recipe.title,
      description: recipe.description,
      categoryId: recipe.categoryId,
      cookingTime: recipe.cookingTime,
      servings: recipe.serving,
      difficulty: recipe.difficulty,
      cuisine: recipe.cuisine,
      calories: recipe.calories,
      protein: recipe.protein,
      carbohydrates: recipe.carbohydrates,
      fat: recipe.fat
    });

    // Set image preview
    if (recipe.imageUrl) {
      this.filePreviewUrl = recipe.imageUrl;
    }

    // Populate allergens
    this.populateFormArray(this.allergens, recipe.allergens || []);

    // Populate diets
    this.populateFormArray(this.diets, recipe.diets || []);

    // Populate tags
    this.populateFormArray(this.tags, recipe.tags || []);

    // Populate ingredients
    this.ingredients.clear();
    recipe.ingredients?.forEach(ing => {
      const parsed = this.parseIngredient(ing);
      this.ingredients.push(this.fb.group({
        name: [parsed.name, Validators.required],
        amount: [parsed.amount, Validators.required],
        unit: [parsed.unit, Validators.required]
      }));
    });
    if (this.ingredients.length === 0) {
      this.ingredients.push(this.createIngredientGroup());
    }

    // Populate instructions
    this.instructions.clear();
    recipe.directions?.forEach((step, index) => {
      this.instructions.push(this.fb.group({
        step: [step, Validators.required],
        order: [index + 1, Validators.required]
      }));
    });
    if (this.instructions.length === 0) {
      this.instructions.push(this.createInstructionGroup());
    }
  }

  private populateFormArray(formArray: FormArray, items: string[]): void {
    formArray.clear();
    items.forEach(item => {
      formArray.push(this.fb.group({ name: [item] }));
    });
    if (formArray.length === 0) {
      formArray.push(this.createCharacteristicGroup());
    }
  }

  private parseIngredient(ingredient: string): { amount: string, unit: string, name: string } {
    const match = ingredient.match(/^(\d+(?:\.\d+)?)\s*([^\d\s]+)\s*(.+)$/);
    if (match) {
      return {
        amount: match[1],
        unit: match[2],
        name: match[3].trim()
      };
    }
    // Fallback: treat whole string as name
    return {
      amount: '',
      unit: '',
      name: ingredient.trim()
    };
  }

  // File upload methods
  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.handleFileSelect(file);
    }
  }

  onFileDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const target = event.target as HTMLElement;
    target.classList.add('dragover');
  }

  onFileDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const target = event.target as HTMLElement;
    target.classList.remove('dragover');
  }

  onFileDropped(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const target = event.target as HTMLElement;
    target.classList.remove('dragover');

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFileSelect(files[0]);
    }
  }

  private handleFileSelect(file: File): void {
    const validation = this.validateFile(file);
    if (!validation.isValid) {
      this.fileError = validation.error || '';
      return;
    }

    this.selectedFile = file;
    this.fileError = '';
    this.createFilePreview(file);
  }

  private validateFile(file: File): { isValid: boolean; error?: string } {
    if (!this.allowedTypes.includes(file.type)) {
      return {
        isValid: false,
        error: 'Невірний формат файлу. Підтримуються лише JPG, PNG та GIF.'
      };
    }

    if (file.size > this.maxFileSize) {
      return {
        isValid: false,
        error: 'Файл занадто великий. Максимальний розмір: 5MB.'
      };
    }

    return { isValid: true };
  }

  private createFilePreview(file: File): void {
    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.filePreviewUrl = e.target.result;
    };
    reader.readAsDataURL(file);
  }

  removeFile(): void {
    this.selectedFile = null;
    this.filePreviewUrl = null;
    this.fileError = '';
    
    const fileInput = document.getElementById('recipeImage') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  // Getters for FormArrays
  get ingredients(): FormArray {
    return this.recipeForm.get('ingredients') as FormArray;
  }

  get instructions(): FormArray {
    return this.recipeForm.get('instructions') as FormArray;
  }

  get allergens(): FormArray {
    return this.recipeForm.get('allergens') as FormArray;
  }

  get diets(): FormArray {
    return this.recipeForm.get('diets') as FormArray;
  }

  get tags(): FormArray {
    return this.recipeForm.get('tags') as FormArray;
  }

  // Add/Remove methods
  addAllergen(): void {
    this.allergens.push(this.createCharacteristicGroup());
  }

  removeAllergen(index: number): void {
    if (this.allergens.length > 1) {
      this.allergens.removeAt(index);
    }
  }

  addDiet(): void {
    this.diets.push(this.createCharacteristicGroup());
  }

  removeDiet(index: number): void {
    if (this.diets.length > 1) {
      this.diets.removeAt(index);
    }
  }

  addTag(): void {
    this.tags.push(this.createCharacteristicGroup());
  }

  removeTag(index: number): void {
    if (this.tags.length > 1) {
      this.tags.removeAt(index);
    }
  }

  addIngredient(): void {
    this.ingredients.push(this.createIngredientGroup());
  }

  removeIngredient(index: number): void {
    if (this.ingredients.length > 1) {
      this.ingredients.removeAt(index);
    }
  }

  addInstruction(): void {
    const newOrder = this.instructions.length + 1;
    const instructionGroup = this.createInstructionGroup();
    instructionGroup.patchValue({ order: newOrder });
    this.instructions.push(instructionGroup);
  }

  removeInstruction(index: number): void {
    if (this.instructions.length > 1) {
      this.instructions.removeAt(index);
      this.instructions.controls.forEach((control, idx) => {
        control.patchValue({ order: idx + 1 });
      });
    }
  }

  // Show/Hide form
  showCreateForm(): void {
    if (!this.authService.isAuthenticated()) {
      alert('Для створення рецепту потрібно увійти в систему');
      return;
    }
    this.showForm = true;
    // Set categoryId if provided via Input
    this.recipeForm.patchValue({
      categoryId: this.category?.id || ''
    });
  }

  hideCreateForm(): void {
    this.showForm = false;
    this.resetForm();
    this.formClosed.emit();
  }

  // Form submission
  onSubmit(): void {
    if (this.recipeForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;
      
      const formData = this.prepareFormData();
      
      console.log('=== FormData content ===');
      formData.forEach((value, key) => {
        console.log(`${key}:`, value);
      });

      if (this.isEditMode && this.recipeToEdit?.id) {
        this.recipeService.updateRecipe(this.recipeToEdit.id, formData).subscribe({
          next: (apiresponse) => {
            this.recipeUpdated.emit(apiresponse.result);
            this.hideCreateForm();
            alert('Рецепт успішно оновлено!');
          },
          error: (error) => {
            console.error('Error updating recipe:', error);
            alert('Помилка при оновленні рецепту. Спробуйте ще раз.');
          },
          complete: () => {
            this.isSubmitting = false;
          }
        });
      } else {
        this.recipeService.createRecipe(formData).subscribe({
          next: (apiresponse) => {
            this.recipeCreated.emit(apiresponse.result);
            this.hideCreateForm();
            alert('Рецепт успішно створено!');
          },
          error: (error) => {
            console.error('Error creating recipe:', error);
            alert('Помилка при створенні рецепту. Спробуйте ще раз.');
          },
          complete: () => {
            this.isSubmitting = false;
          }
        });
      }
    }
  }

  private prepareFormData(): FormData {
    const formData = new FormData();
    const formValue = this.recipeForm.value;

    if (this.recipeToEdit) {
      formData.append('Id', this.recipeToEdit.id);
    }
    formData.append('title', formValue.title || '');
    formData.append('description', formValue.description || '');
    formData.append('categoryId', formValue.categoryId || '');
    formData.append('cookingTime', formValue.cookingTime?.toString() || '0');
    formData.append('difficulty', formValue.difficulty || 'Easy');
    formData.append('serving', formValue.servings?.toString() || '1');
    formData.append('calories', formValue.calories?.toString() || '0');
    formData.append('protein', formValue.protein?.toString() || '0');
    formData.append('fat', formValue.fat?.toString() || '0');
    formData.append('carbohydrates', formValue.carbohydrates?.toString() || '0');
    formData.append('cuisine', formValue.cuisine || '');
    
    // Ingredients
    const ingredientsList = formValue.ingredients
      .filter((ing: any) => ing.name && ing.name.trim())
      .map((ing: any) => `${ing.amount} ${ing.unit} ${ing.name}`.trim());
    
    ingredientsList.forEach((ingredient: string, index: number) => {
      formData.append(`ingredients[${index}]`, ingredient);
    });

    // Directions
    const directionsList = formValue.instructions
      .filter((inst: any) => inst.step && inst.step.trim())
      .map((inst: any) => inst.step.trim());
      
    directionsList.forEach((direction: string, index: number) => {
      formData.append(`directions[${index}]`, direction);
    });

    // Allergens
    const allergensList = this.extractCharacteristicNames(formValue.allergens);
    allergensList.forEach((allergen: string, index: number) => {
      formData.append(`allergens[${index}]`, allergen);
    });

    // Diets
    const dietsList = this.extractCharacteristicNames(formValue.diets);
    dietsList.forEach((diet: string, index: number) => {
      formData.append(`diets[${index}]`, diet);
    });

    // Tags
    const tagsList = this.extractCharacteristicNames(formValue.tags);
    tagsList.forEach((tag: string, index: number) => {
      formData.append(`tags[${index}]`, tag);
    });

    // Image file if changed
    if (this.selectedFile) {
      formData.append('image', this.selectedFile, this.selectedFile.name);
    }

    return formData;
  }

  extractCharacteristicNames(characteristics: any[]): string[] {
    return characteristics
      .map(char => char.name?.trim())
      .filter(name => name && name.length > 0);
  }

  resetForm(): void {
    this.recipeForm.reset();
    
    this.removeFile();
    
    this.resetFormArray(this.ingredients, this.createIngredientGroup);
    this.resetFormArray(this.instructions, this.createInstructionGroup);
    this.resetFormArray(this.allergens, this.createCharacteristicGroup);
    this.resetFormArray(this.diets, this.createCharacteristicGroup);
    this.resetFormArray(this.tags, this.createCharacteristicGroup);
    
    this.instructions.at(0).patchValue({ order: 1 });
    this.recipeForm.patchValue({
      categoryId: this.category?.id || '',
      difficulty: 'Easy',
      servings: 1
    });
  }

  resetFormArray(formArray: FormArray, createGroupFn: () => FormGroup): void {
    while (formArray.length > 0) {
      formArray.removeAt(0);
    }
    formArray.push(createGroupFn.call(this));
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.recipeForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.recipeForm.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) return 'Це поле є обов’язковим';
      if (field.errors['minlength']) return `Мінімальна довжина: ${field.errors['minlength'].requiredLength} символів`;
      if (field.errors['min']) return `Мінімальне значення: ${field.errors['min'].min}`;
    }
    return '';
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }
}