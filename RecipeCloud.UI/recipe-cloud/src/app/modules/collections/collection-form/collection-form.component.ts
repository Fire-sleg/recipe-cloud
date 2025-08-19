import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { CollectionService } from '../../../core/services/collection.service';
import { AuthService } from '../../../core/services/auth.service';
import { RecipeService } from '../../../core/services/recipe.service';
import { Collection } from '../../../core/models/collection.model';
import { Recipe } from '../../../core/models/recipe.model';
import { APIResponse, PagedResponse } from '../../../core/models/paged-response';

@Component({
  selector: 'app-collection-form',
  templateUrl: './collection-form.component.html',
  styleUrls: ['./collection-form.component.css']
})
export class CollectionFormComponent implements OnInit {
  @Output() collectionCreated = new EventEmitter<Collection>();
  @Output() formClosed = new EventEmitter<void>();

  collectionForm: FormGroup;
  isSubmitting = false;
  showForm = false;
  availableRecipes: Recipe[] = [];
  userName?: string;
  
  // File upload properties
  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  fileError: string = '';
  maxFileSize = 5 * 1024 * 1024; // 5MB
  allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];

  constructor(
    private fb: FormBuilder,
    private collectionService: CollectionService,
    private recipeService: RecipeService,
    private authService: AuthService
  ) {
    this.collectionForm = this.createForm();
  }

    ngOnInit(): void {
        this.recipeService.getRecipes(1, 100)
        .subscribe({
            next: (apiresponse: APIResponse<PagedResponse<Recipe>>) => {
                var response = apiresponse.result;
                // console.log('API Response:', response); 
                this.availableRecipes = response.data;
                // console.log('Available Recipes Length:', this.availableRecipes.length);
                // console.log(this.availableRecipes);
            },
            error: (err) => {
                console.error('Error fetching recipes:', err);
            }
        });
    }



  createForm(): FormGroup {
    return this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      // description: ['', [Validators.required, Validators.minLength(10)]],
      recipes: this.fb.array([this.createRecipeGroup()])
    });
  }

  createRecipeGroup(): FormGroup {
    return this.fb.group({
      recipeId: ['', Validators.required]
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
    
    const fileInput = document.getElementById('collectionImage') as HTMLInputElement;
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

  // Getters
  get recipes(): FormArray {
    return this.collectionForm.get('recipes') as FormArray;
  }

  // Recipe methods
  addRecipe(): void {
    this.recipes.push(this.createRecipeGroup());
  }

  removeRecipe(index: number): void {
    if (this.recipes.length > 1) {
      this.recipes.removeAt(index);
    }
  }

  // Show/Hide form
  showCreateForm(): void {
    if (!this.authService.isAuthenticated()) {
      alert('Для створення колекції потрібно увійти в систему');
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
    if (this.collectionForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;
      
      const formData = this.prepareFormData();
      // console.log('=== FormData content ===');
      // formData.forEach((value, key) => {
      //   console.log(`${key}:`, value);
      // });
      this.collectionService.createCollection(formData).subscribe({
        next: (collection) => {
          this.collectionCreated.emit(collection);
          this.hideCreateForm();
          alert('Колекцію успішно створено!');
        },
        error: (error) => {
          console.error('Error creating collection:', error);
          alert('Помилка при створенні колекції. Спробуйте ще раз.');
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
    }
  }

  private prepareFormData(): FormData {
    const formData = new FormData();
    const formValue = this.collectionForm.value;

    formData.append('title', formValue.title || '');
    // formData.append('description', formValue.description || '');
    
    const recipeIds = formValue.recipes
      .map((recipe: any) => recipe.recipeId)
      .filter((id: string) => id && id.trim());

    this.authService.currentUser$.subscribe(r =>{
      this.userName = r?.username;
    })
    if(this.userName){
      formData.append('createdByUsername', this.userName);
    }

    
    recipeIds.forEach((recipeId: string, index: number) => {
      formData.append(`recipeIds[${index}]`, recipeId);
    });

    // if (this.selectedFile) {
    //   formData.append('image', this.selectedFile, this.selectedFile.name);
    // }

    return formData;
  }

  resetForm(): void {
    this.collectionForm.reset();
    this.removeFile();
    this.resetFormArray(this.recipes, this.createRecipeGroup);
  }

  resetFormArray(formArray: FormArray, createGroupFn: () => FormGroup): void {
    while (formArray.length > 0) {
      formArray.removeAt(0);
    }
    formArray.push(createGroupFn.call(this));
  }

  // Helper methods
  isFieldInvalid(fieldName: string): boolean {
    const field = this.collectionForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.collectionForm.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) return `${fieldName} є обов'язковим`;
      if (field.errors['minlength']) return `Мінімальна довжина: ${field.errors['minlength'].requiredLength}`;
    }
    return '';
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }
}