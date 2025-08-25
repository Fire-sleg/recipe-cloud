// import { Component, OnInit, Output, EventEmitter, DestroyRef, inject, ChangeDetectorRef } from '@angular/core';
// import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
// import { finalize, take } from 'rxjs/operators';
// import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
// import { CollectionService } from '../../../core/services/collection.service';
// import { AuthService } from '../../../core/services/auth.service';
// import { RecipeService } from '../../../core/services/recipe.service';
// import { Collection } from '../../../core/models/collection.model';
// import { Recipe } from '../../../core/models/recipe.model';
// import { APIResponse, PagedResponse } from '../../../core/models/paged-response';

// @Component({
//   selector: 'app-collection-form',
//   templateUrl: './collection-form.component.html',
//   styleUrls: ['./collection-form.component.css']
// })
// export class CollectionFormComponent implements OnInit {
//   @Output() collectionCreated = new EventEmitter<Collection>(); // необов'язково, залишив для зворотної сумісності
//   @Output() formClosed = new EventEmitter<void>();

//   collectionForm: FormGroup;
//   isSubmitting = false;
//   showForm = false;
//   availableRecipes: Recipe[] = [];
//   userName?: string;

//   // File upload (залишено як у тебе)
//   selectedFile: File | null = null;
//   filePreviewUrl: string | null = null;
//   fileError: string = '';
//   maxFileSize = 5 * 1024 * 1024; // 5MB
//   allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];

//   // destroy ref for auto-unsubscribe
//   private destroyRef: DestroyRef = inject(DestroyRef);

//   constructor(
//     private fb: FormBuilder,
//     private collectionService: CollectionService,
//     private recipeService: RecipeService,
//     private authService: AuthService,
//     private cdr: ChangeDetectorRef
//   ) {
//     this.collectionForm = this.createForm();
//   }

//   ngOnInit(): void {
//     // 1) завантажуємо рецепти (разово)
//     this.recipeService.getRecipes(1, 100)
//       .pipe(take(1), takeUntilDestroyed(this.destroyRef))
//       .subscribe({
//         next: (apiresponse: APIResponse<PagedResponse<Recipe>>) => {
//           const response = apiresponse.result;
//           this.availableRecipes = response.data;
//         },
//         error: (err) => console.error('Error fetching recipes:', err)
//       });

//     // 2) читаємо поточного користувача (і тримаємо в полі)
//     this.authService.currentUser$
//       .pipe(takeUntilDestroyed(this.destroyRef))
//       .subscribe(u => {
//         this.userName = u?.username ?? undefined;
//       });
//   }

//   // ------- form builders -------
//   createForm(): FormGroup {
//     return this.fb.group({
//       title: ['', [Validators.required, Validators.minLength(3)]],
//       recipes: this.fb.array([this.createRecipeGroup()])
//     });
//   }

//   createRecipeGroup(): FormGroup {
//     return this.fb.group({
//       recipeId: ['', Validators.required]
//     });
//   }

//   // ------- getters -------
//   get recipes(): FormArray {
//     return this.collectionForm.get('recipes') as FormArray;
//   }

//   // ------- list helpers -------
//   trackByIndex = (_: number, __: unknown) => _;
//   trackByRecipeId = (_: number, r: Recipe) => r.id;

//   // ------- file upload (без змін з твоєї логіки) -------
//   onFileSelected(event: any): void {
//     const file = event.target.files[0];
//     if (file) this.handleFileSelect(file);
//   }
//   onFileDragOver(event: DragEvent): void {
//     event.preventDefault(); event.stopPropagation();
//     (event.target as HTMLElement).classList.add('dragover');
//   }
//   onFileDragLeave(event: DragEvent): void {
//     event.preventDefault(); event.stopPropagation();
//     (event.target as HTMLElement).classList.remove('dragover');
//   }
//   onFileDropped(event: DragEvent): void {
//     event.preventDefault(); event.stopPropagation();
//     (event.target as HTMLElement).classList.remove('dragover');
//     const files = event.dataTransfer?.files;
//     if (files && files.length > 0) this.handleFileSelect(files[0]);
//   }
//   private handleFileSelect(file: File): void {
//     const validation = this.validateFile(file);
//     if (!validation.isValid) { this.fileError = validation.error || ''; return; }
//     this.selectedFile = file; this.fileError = ''; this.createFilePreview(file);
//   }
//   private validateFile(file: File): { isValid: boolean; error?: string } {
//     if (!this.allowedTypes.includes(file.type)) {
//       return { isValid: false, error: 'Невірний формат файлу. Підтримуються лише JPG, PNG та GIF.' };
//     }
//     if (file.size > this.maxFileSize) {
//       return { isValid: false, error: 'Файл занадто великий. Максимальний розмір: 5MB.' };
//     }
//     return { isValid: true };
//   }
//   private createFilePreview(file: File): void {
//     const reader = new FileReader();
//     reader.onload = (e: any) => this.filePreviewUrl = e.target.result;
//     reader.readAsDataURL(file);
//   }
//   removeFile(): void {
//     this.selectedFile = null; this.filePreviewUrl = null; this.fileError = '';
//     const fileInput = document.getElementById('collectionImage') as HTMLInputElement;
//     if (fileInput) fileInput.value = '';
//   }
//   formatFileSize(bytes: number): string {
//     if (bytes === 0) return '0 Bytes';
//     const k = 1024, sizes = ['Bytes', 'KB', 'MB', 'GB'];
//     const i = Math.floor(Math.log(bytes) / Math.log(k));
//     return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
//   }

//   // ------- recipes controls -------
//   addRecipe(): void {
//     this.recipes.push(this.createRecipeGroup());
//   }
//   removeRecipe(index: number): void {
//     if (this.recipes.length > 1) this.recipes.removeAt(index);
//   }

//   // ------- modal controls -------
//   showCreateForm(): void {
//     if (!this.authService.isAuthenticated()) {
//       alert('Для створення колекції потрібно увійти в систему');
//       return;
//     }
//     this.showForm = true;
//     // фокус усередину модалки для доступності
//     setTimeout(() => {
//       const container = document.querySelector('.collection-form-container') as HTMLElement;
//       container?.focus();
//     }, 0);
//   }

//   onOverlayClick(): void {
//     if (!this.isSubmitting) this.hideCreateForm();
//   }

//   onCloseClicked(): void {
//     if (!this.isSubmitting) this.hideCreateForm();
//   }

//   onEscape(): void {
//     if (!this.isSubmitting) this.hideCreateForm();
//   }

//   hideCreateForm(): void {
//     this.showForm = false;
//     this.cdr.detectChanges();  // NEW: Force immediate view update to remove form from DOM
//     this.resetForm();
//     this.formClosed.emit();
//   }

//   onSubmit(): void {
//     if (this.collectionForm.valid && !this.isSubmitting) {
//       this.isSubmitting = true;

//       const formData = this.prepareFormData();

//       this.collectionService.createCollection(formData)
//         .pipe(
//           finalize(() => {
//             this.isSubmitting = false;  // Always reset, even on error
//           })
//         )
//         .subscribe({
//           next: (collection) => {
//             if (collection) {
//               this.collectionCreated.emit(collection);
//               this.hideCreateForm();
//               this.collectionService.getByUserId(this.authService.getCurrentUserId()!).subscribe(); // Refresh data
//               this.collectionService.successMessage$.next('Колекцію успішно створено!');
//             }
//           },
//           error: (error) => {
//             console.error('Error creating collection:', error);
//             this.collectionService.errorMessage$.next('Помилка при створенні колекції. Спробуйте ще раз.');
//           }
//           // No complete handler needed (finalize handles cleanup)
//         });
//     }
//   }


//   private prepareFormData(): FormData {
//     const formData = new FormData();
//     const formValue = this.collectionForm.value;

//     formData.append('title', formValue.title || '');

//     const recipeIds = (formValue.recipes || [])
//       .map((r: any) => r?.recipeId)
//       .filter((id: string) => !!id && id.trim());

//     // userName уже встановлено у ngOnInit, без зайвих підписок тут
//     if (this.userName) {
//       formData.append('createdByUsername', this.userName);
//     }

//     recipeIds.forEach((recipeId: string, index: number) => {
//       formData.append(`recipeIds[${index}]`, recipeId);
//     });

//     // якщо потрібно повертати завантаження зображення — розкоментувати
//     // if (this.selectedFile) {
//     //   formData.append('image', this.selectedFile, this.selectedFile.name);
//     // }

//     return formData;
//   }

//   resetForm(): void {
//     this.collectionForm.reset();
//     // мінімально відновлюємо структуру
//     this.resetFormArray(this.recipes, this.createRecipeGroup);
//     this.collectionForm.markAsPristine();
//     this.collectionForm.markAsUntouched();
//     this.removeFile();
//   }

//   resetFormArray(formArray: FormArray, createGroupFn: () => FormGroup): void {
//     while (formArray.length > 0) formArray.removeAt(0);
//     formArray.push(createGroupFn.call(this));
//   }

//   // ------- validation helpers -------
//   isFieldInvalid(fieldName: string): boolean {
//     const field = this.collectionForm.get(fieldName);
//     return !!(field && field.invalid && (field.dirty || field.touched));
//   }

//   getFieldError(fieldName: string): string {
//     const field = this.collectionForm.get(fieldName);
//     if (field?.errors) {
//       if (field.errors['required']) return `${fieldName} є обов'язковим`;
//       if (field.errors['minlength']) return `Мінімальна довжина: ${field.errors['minlength'].requiredLength}`;
//     }
//     return '';
//   }

//   get isAuthenticated(): boolean {
//     return this.authService.isAuthenticated();
//   }
// }
// CollectionFormComponent TypeScript

import { Component, OnInit, Output, EventEmitter, Input, OnChanges, SimpleChanges, DestroyRef, inject, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { finalize, take } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
export class CollectionFormComponent implements OnInit, OnChanges {
  @Input() collectionToEdit: Collection | null = null;

  @Output() collectionCreated = new EventEmitter<Collection>();
  @Output() collectionUpdated = new EventEmitter<Collection>();
  @Output() formClosed = new EventEmitter<void>();

  collectionForm: FormGroup;
  isSubmitting = false;
  showForm = false;
  isEditMode = false;
  availableRecipes: Recipe[] = [];
  userName?: string;

  // File upload properties
  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  fileError: string = '';
  maxFileSize = 5 * 1024 * 1024; // 5MB
  allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];

  // destroy ref for auto-unsubscribe
  private destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private fb: FormBuilder,
    private collectionService: CollectionService,
    private recipeService: RecipeService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    this.collectionForm = this.createForm();
  }

  ngOnInit(): void {
    // 1) завантажуємо рецепти (разово)
    this.recipeService.getRecipes(1, 100)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (apiresponse: APIResponse<PagedResponse<Recipe>>) => {
          const response = apiresponse.result;
          this.availableRecipes = response.data;
        },
        error: (err) => console.error('Error fetching recipes:', err)
      });

    // 2) читаємо поточного користувача (і тримаємо в полі)
    this.authService.currentUser$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(u => {
        this.userName = u?.username ?? undefined;
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['collectionToEdit'] && this.collectionToEdit) {
      this.isEditMode = true;
      this.showForm = true;
      this.populateForm(this.collectionToEdit);
    }
  }

  // ------- form builders -------
  createForm(): FormGroup {
    return this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      recipes: this.fb.array([this.createRecipeGroup()])
    });
  }

  createRecipeGroup(): FormGroup {
    return this.fb.group({
      recipeId: ['', Validators.required]
    });
  }

  // Populate form with collection data for editing
  private populateForm(collection: Collection): void {
    this.collectionForm.patchValue({
      title: collection.title
    });

    // Populate recipes
    this.recipes.clear();
    collection.recipes?.forEach(rec => {
      this.recipes.push(this.fb.group({
        recipeId: [rec.id, Validators.required]
      }));
    });
    if (this.recipes.length === 0) {
      this.recipes.push(this.createRecipeGroup());
    }
  }

  // ------- getters -------
  get recipes(): FormArray {
    return this.collectionForm.get('recipes') as FormArray;
  }

  // ------- list helpers -------
  trackByIndex = (_: number, __: unknown) => _;
  trackByRecipeId = (_: number, r: Recipe) => r.id;

  // ------- file upload (без змін з твоєї логіки) -------
  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) this.handleFileSelect(file);
  }
  onFileDragOver(event: DragEvent): void {
    event.preventDefault(); event.stopPropagation();
    (event.target as HTMLElement).classList.add('dragover');
  }
  onFileDragLeave(event: DragEvent): void {
    event.preventDefault(); event.stopPropagation();
    (event.target as HTMLElement).classList.remove('dragover');
  }
  onFileDropped(event: DragEvent): void {
    event.preventDefault(); event.stopPropagation();
    (event.target as HTMLElement).classList.remove('dragover');
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) this.handleFileSelect(files[0]);
  }
  private handleFileSelect(file: File): void {
    const validation = this.validateFile(file);
    if (!validation.isValid) { this.fileError = validation.error || ''; return; }
    this.selectedFile = file; this.fileError = ''; this.createFilePreview(file);
  }
  private validateFile(file: File): { isValid: boolean; error?: string } {
    if (!this.allowedTypes.includes(file.type)) {
      return { isValid: false, error: 'Невірний формат файлу. Підтримуються лише JPG, PNG та GIF.' };
    }
    if (file.size > this.maxFileSize) {
      return { isValid: false, error: 'Файл занадто великий. Максимальний розмір: 5MB.' };
    }
    return { isValid: true };
  }
  private createFilePreview(file: File): void {
    const reader = new FileReader();
    reader.onload = (e: any) => this.filePreviewUrl = e.target.result;
    reader.readAsDataURL(file);
  }
  removeFile(): void {
    this.selectedFile = null; this.filePreviewUrl = null; this.fileError = '';
    const fileInput = document.getElementById('collectionImage') as HTMLInputElement;
    if (fileInput) fileInput.value = '';
  }
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024, sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  // ------- recipes controls -------
  addRecipe(): void {
    this.recipes.push(this.createRecipeGroup());
  }
  removeRecipe(index: number): void {
    if (this.recipes.length > 1) this.recipes.removeAt(index);
  }

  // ------- modal controls -------
  showCreateForm(): void {
    if (!this.authService.isAuthenticated()) {
      alert('Для створення колекції потрібно увійти в систему');
      return;
    }
    this.showForm = true;
    // фокус усередину модалки для доступності
    setTimeout(() => {
      const container = document.querySelector('.collection-form-container') as HTMLElement;
      container?.focus();
    }, 0);
  }

  onOverlayClick(): void {
    if (!this.isSubmitting) this.hideCreateForm();
  }

  onCloseClicked(): void {
    if (!this.isSubmitting) this.hideCreateForm();
  }

  onEscape(): void {
    if (!this.isSubmitting) this.hideCreateForm();
  }

  hideCreateForm(): void {
    this.showForm = false;
    this.isEditMode = false;
    this.cdr.detectChanges();  // Force immediate view update to remove form from DOM
    this.resetForm();
    this.formClosed.emit();
  }

  onSubmit(): void {
    if (this.collectionForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;

      const formData = this.prepareFormData();
      console.log('=== FormData content ===');
      formData.forEach((value, key) => {
        console.log(`${key}:`, value);
      });

      if (this.isEditMode && this.collectionToEdit?.id) {
        this.collectionService.updateCollection(this.collectionToEdit.id, formData)
          .pipe(
            finalize(() => {
              this.isSubmitting = false;
            })
          )
          .subscribe({
            next: (updatedCollection) => {
              if (updatedCollection) {
                this.collectionUpdated.emit(updatedCollection);
                this.hideCreateForm();
                this.collectionService.getByUserId(this.authService.getCurrentUserId()!).subscribe(); // Refresh data
                this.collectionService.successMessage$.next('Колекцію успішно оновлено!');
              }
            },
            error: (error) => {
              console.error('Error updating collection:', error);
              this.collectionService.errorMessage$.next('Помилка при оновленні колекції. Спробуйте ще раз.');
            }
          });
      } else {
        this.collectionService.createCollection(formData)
          .pipe(
            finalize(() => {
              this.isSubmitting = false;
            })
          )
          .subscribe({
            next: (collection) => {
              if (collection) {
                this.collectionCreated.emit(collection);
                this.hideCreateForm();
                this.collectionService.getByUserId(this.authService.getCurrentUserId()!).subscribe(); // Refresh data
                this.collectionService.successMessage$.next('Колекцію успішно створено!');
              }
            },
            error: (error) => {
              console.error('Error creating collection:', error);
              this.collectionService.errorMessage$.next('Помилка при створенні колекції. Спробуйте ще раз.');
            }
          });
      }
    }
  }

  private prepareFormData(): FormData {
    const formData = new FormData();
    const formValue = this.collectionForm.value;

    if (this.isEditMode && this.collectionToEdit) {
      formData.append('id', this.collectionToEdit.id);
    }

    formData.append('title', formValue.title || '');

    const recipeIds = (formValue.recipes || [])
      .map((r: any) => r?.recipeId)
      .filter((id: string) => !!id && id.trim());

    // userName уже встановлено у ngOnInit, без зайвих підписок тут
    if (this.userName) {
      formData.append('createdByUsername', this.userName);
    }

    recipeIds.forEach((recipeId: string, index: number) => {
      formData.append(`recipeIds[${index}]`, recipeId);
    });

    // якщо потрібно повертати завантаження зображення — розкоментувати
    // if (this.selectedFile) {
    //   formData.append('image', this.selectedFile, this.selectedFile.name);
    // }

    return formData;
  }

  resetForm(): void {
    this.collectionForm.reset();
    // мінімально відновлюємо структуру
    this.resetFormArray(this.recipes, this.createRecipeGroup);
    this.collectionForm.markAsPristine();
    this.collectionForm.markAsUntouched();
    this.removeFile();
  }

  resetFormArray(formArray: FormArray, createGroupFn: () => FormGroup): void {
    while (formArray.length > 0) formArray.removeAt(0);
    formArray.push(createGroupFn.call(this));
  }

  // ------- validation helpers -------
  isFieldInvalid(fieldName: string): boolean {
    const field = this.collectionForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.collectionForm.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) return 'Це поле є обов’язковим';
      if (field.errors['minlength']) return `Мінімальна довжина: ${field.errors['minlength'].requiredLength} символів`;
    }
    return '';
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }
}