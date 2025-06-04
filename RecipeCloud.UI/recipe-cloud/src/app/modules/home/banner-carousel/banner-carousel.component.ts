import { animate, style, transition, trigger } from '@angular/animations';
import { Component,  Input,  OnInit } from '@angular/core';

@Component({
  selector: 'app-banner-carousel',
  templateUrl: './banner-carousel.component.html',
  styleUrls: ['./banner-carousel.component.css'],
  animations: [
    trigger('slideAnimation', [
      transition('inactive => active', [
        style({
          opacity: 0,
          transform: 'scale(1.1)'
        }),
        animate('600ms ease-out', style({
          opacity: 1,
          transform: 'scale(1)'
        }))
      ]),
      transition('active => inactive', [
        animate('400ms ease-in', style({
          opacity: 0,
          transform: 'scale(0.95)'
        }))
      ])
    ])
  ]
})
export class BannerCarouselComponent implements OnInit {
  images = [
    'assets/image1.jpg',
    'assets/image2.jpg'
  ];

  @Input() autoPlay: boolean = true;
  @Input() interval: number = 5000; // 5 секунд

  currentIndex: number = 0;
  private autoPlayInterval: any;

  ngOnInit() {
    if (this.autoPlay && this.images.length > 1) {
      this.startAutoPlay();
    }
  }

  ngOnDestroy() {
    this.stopAutoPlay();
  }

  nextSlide() {
    this.currentIndex = (this.currentIndex + 1) % this.images.length;
    this.resetAutoPlay();
  }

  previousSlide() {
    this.currentIndex = this.currentIndex === 0 ? this.images.length - 1 : this.currentIndex - 1;
    this.resetAutoPlay();
  }

  goToSlide(index: number) {
    this.currentIndex = index;
    this.resetAutoPlay();
  }

  private startAutoPlay() {
    this.autoPlayInterval = setInterval(() => {
      this.currentIndex = (this.currentIndex + 1) % this.images.length;
    }, this.interval);
  }

  private stopAutoPlay() {
    if (this.autoPlayInterval) {
      clearInterval(this.autoPlayInterval);
    }
  }

  private resetAutoPlay() {
    if (this.autoPlay && this.images.length > 1) {
      this.stopAutoPlay();
      this.startAutoPlay();
    }
  }
}