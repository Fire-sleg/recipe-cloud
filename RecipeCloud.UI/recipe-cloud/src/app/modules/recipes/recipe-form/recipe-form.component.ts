import { Component, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { RecipeService } from '../../../core/services/recipe.service';
import { AuthService } from '../../../core/services/auth.service';
import { Recipe } from '../../../core/models/recipe.model';
import { Category } from '../../../core/models/category.model';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-recipe-form',
  templateUrl: './recipe-form.component.html',
  styleUrls: ['./recipe-form.component.css']
})
export class RecipeFormComponent implements OnInit {
  @Input() category: Category | null = null;
  @Output() recipeCreated = new EventEmitter<Recipe>();
  @Output() formClosed = new EventEmitter<void>();

  recipeForm: FormGroup;
  isSubmitting = false;
  showForm = false;
  
  // File upload properties
  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  fileError: string = '';
  maxFileSize = 5 * 1024 * 1024; // 5MB
  allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];

  constructor(
    private fb: FormBuilder,
    private recipeService: RecipeService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    const navigation = this.router.getCurrentNavigation();
    this.category = navigation?.extras?.state?.['category'];
    this.recipeForm = this.createForm();
  }

  ngOnInit(): void {
    // Component initialization
  }

  createForm(): FormGroup {
    return this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      categoryId: [this.category?.id || '', Validators.required],
      cookingTime: [0, [Validators.required, Validators.min(1)]],
      servings: [1, [Validators.required, Validators.min(1)]],
      difficulty: ['Easy', Validators.required],
      
      // Nutritional Information
      calories: [0, [Validators.min(0)]],
      protein: [0, [Validators.min(0)]],
      carbohydrates: [0, [Validators.min(0)]],
      fat: [0, [Validators.min(0)]],
      
      // Characteristics as FormArrays
      allergens: this.fb.array([this.createCharacteristicGroup()]),
      cuisines: this.fb.array([this.createCharacteristicGroup()]),
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
    // Check file type
    if (!this.allowedTypes.includes(file.type)) {
      return {
        isValid: false,
        error: 'Невірний формат файлу. Підтримуються лише JPG, PNG та GIF.'
      };
    }

    // Check file size
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
    
    // Reset file input
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

  get cuisines(): FormArray {
    return this.recipeForm.get('cuisines') as FormArray;
  }

  get diets(): FormArray {
    return this.recipeForm.get('diets') as FormArray;
  }

  get tags(): FormArray {
    return this.recipeForm.get('tags') as FormArray;
  }

  // Add/Remove methods for characteristics
  addAllergen(): void {
    this.allergens.push(this.createCharacteristicGroup());
  }

  removeAllergen(index: number): void {
    if (this.allergens.length > 1) {
      this.allergens.removeAt(index);
    }
  }

  addCuisine(): void {
    this.cuisines.push(this.createCharacteristicGroup());
  }

  removeCuisine(index: number): void {
    if (this.cuisines.length > 1) {
      this.cuisines.removeAt(index);
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

  // Ingredients methods
  addIngredient(): void {
    this.ingredients.push(this.createIngredientGroup());
  }

  removeIngredient(index: number): void {
    if (this.ingredients.length > 1) {
      this.ingredients.removeAt(index);
    }
  }

  // Instructions methods
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
      
      // this.recipeService.createRecipeWithImage(formData).subscribe({
      //   next: (recipe) => {
      //     this.recipeCreated.emit(recipe);
      //     this.hideCreateForm();
      //     alert('Рецепт успішно створено!');
      //   },
      //   error: (error) => {
      //     console.error('Error creating recipe:', error);
      //     alert('Помилка при створенні рецепту. Спробуйте ще раз.');
      //   },
      //   complete: () => {
      //     this.isSubmitting = false;
      //   }
      // });
    }
  }

  prepareFormData(): FormData {
    const formValue = this.recipeForm.value;
    const formData = new FormData();
    
    // Add image file if selected
    if (this.selectedFile) {
      formData.append('recipeImage', this.selectedFile);
    }
    
    // Extract names from characteristic arrays and filter empty ones
    const allergens = this.extractCharacteristicNames(formValue.allergens);
    const cuisines = this.extractCharacteristicNames(formValue.cuisines);
    const diets = this.extractCharacteristicNames(formValue.diets);
    const tags = this.extractCharacteristicNames(formValue.tags);

    // Add form data as JSON or individual fields
    const recipeData = {
      ...formValue,
      allergens,
      cuisines,
      diets,
      tags,
      isUserCreated: true
    };

    // Remove image-related fields from JSON data
    delete recipeData.imageUrl;

    // Add recipe data as JSON string
    formData.append('recipeData', JSON.stringify(recipeData));
    
    return formData;
  }

  extractCharacteristicNames(characteristics: any[]): string[] {
    return characteristics
      .map(char => char.name?.trim())
      .filter(name => name && name.length > 0);
  }

  resetForm(): void {
    this.recipeForm.reset();
    
    // Reset file upload
    this.removeFile();
    
    // Reset all arrays to have one empty item
    this.resetFormArray(this.ingredients, this.createIngredientGroup);
    this.resetFormArray(this.instructions, this.createInstructionGroup);
    this.resetFormArray(this.allergens, this.createCharacteristicGroup);
    this.resetFormArray(this.cuisines, this.createCharacteristicGroup);
    this.resetFormArray(this.diets, this.createCharacteristicGroup);
    this.resetFormArray(this.tags, this.createCharacteristicGroup);
    
    // Set default values
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

  // Helper methods
  isFieldInvalid(fieldName: string): boolean {
    const field = this.recipeForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.recipeForm.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) return `${fieldName} є обов'язковим`;
      if (field.errors['minlength']) return `Мінімальна довжина: ${field.errors['minlength'].requiredLength}`;
      if (field.errors['min']) return `Мінімальне значення: ${field.errors['min'].min}`;
    }
    return '';
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }
}