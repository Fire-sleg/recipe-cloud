import { Component } from '@angular/core';
import { Category } from '../../../core/models/category.model';
import { HomeService } from '../../../core/services/home.service';



@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {
  categories: Category[] = [];

  constructor(private homeService: HomeService ) {}

  ngOnInit(): void {
    this.getBaseCategories();
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

  getBaseCategories(): void {
    this.homeService.getBaseCategories().subscribe({
      next: response => {
        this.categories = response
      },
      error: error => console.error(error)
    });

  }
  
}
