import { Component, OnInit } from '@angular/core';
// import { SwiperOptions } from 'swiper';

@Component({
  selector: 'app-banner-carousel',
  templateUrl: './banner-carousel.component.html',
  styleUrls: ['./banner-carousel.component.css']
})
export class BannerCarouselComponent implements OnInit {
//   config: SwiperOptions = {
//     slidesPerView: 1,
//     spaceBetween: 0,
//     autoplay: { delay: 5000 },
//     pagination: { clickable: true },
//     navigation: true
//   };

  banners = [
    { image: 'assets/images/promo1.jpg', title: 'Осінні рецепти', link: '/collections/autumn' },
    { image: 'assets/images/promo2.jpg', title: 'Святкове меню', link: '/collections/christmas' },
    { image: 'assets/images/promo3.jpg', title: 'Веганські новинки', link: '/collections/vegan' }
  ];

  ngOnInit(): void { }
}