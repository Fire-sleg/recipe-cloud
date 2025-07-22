import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, FormControl } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { UserPreferences } from '../../../core/models/user-preferences.model';

@Component({
  selector: 'app-user-preferences',
  templateUrl: './user-preferences.component.html',
  styleUrls: ['./user-preferences.component.css']
})
export class UserPreferencesComponent implements OnInit {
  profileForm!: FormGroup;
  isSubmitting = false;

  availableDiets: string[] = [
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
    "Латиноамериканська",
    "Європейська",
    "Міжнародна",
    "Грецька",
    "Азійська",
    "Українська"
  ];

  constructor(
    private fb: FormBuilder, 
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.loadUserProfile();
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

  private getFormData(): UserPreferences {
    const formValue = this.profileForm.value;
    const userId = this.authService.getCurrentUserId();
    
    return {
      userId: userId,
      dietaryPreferences: this.getSelectedOptions(formValue.diets, this.availableDiets),
      allergens: this.getSelectedOptions(formValue.allergens, this.availableAllergens),
      favoriteCuisines: this.getSelectedOptions(formValue.cuisines, this.availableCuisines)
    };
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
}