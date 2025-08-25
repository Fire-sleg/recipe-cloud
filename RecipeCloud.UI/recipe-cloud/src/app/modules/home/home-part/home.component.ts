import { Component } from '@angular/core';
import { Category } from '../../../core/models/category.model';
import { HomeService } from '../../../core/services/home.service';
import { AuthService } from '../../../core/services/auth.service';
import { Observable } from 'rxjs';



@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {

  // categories: Category[] = [];
  categories$!: Observable<Category[]>;

  isAuthenticated: boolean = false;

  constructor(private homeService: HomeService, private authService: AuthService ) {}

  ngOnInit(): void {
    this.isAuthenticated = this.authService.isAuthenticated();
    this.categories$ = this.homeService.baseCategories$;
    this.homeService.getBaseCategories().subscribe();
    // this.getBaseCategories();
  }

  // getName(): void{
  //   this.authService.getFirstName().subscribe({
  //     next: response =>{
  //       this.name = response;
  //       //console.log(response);
  //     },
  //     error: err => {
  //       console.error('Error fetching name', err);
  //     }
  //   })
  // }

  // getBaseCategories(): void {
  //   this.homeService.getBaseCategories().subscribe({
  //     next: response => {
  //       this.categories = response
  //     },
  //     error: error => console.error(error)
  //   });

  // }
  
}
