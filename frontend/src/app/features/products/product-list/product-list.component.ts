import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ProductService } from '../../../services/product.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  readonly productService = inject(ProductService);

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getAll(
      this.productService.currentPage(),
      this.productService.pageSize()
    ).subscribe();
  }

  goToPage(page: number): void {
    this.productService.getAll(page, this.productService.pageSize()).subscribe();
  }

  previousPage(): void {
    if (this.productService.hasPreviousPage()) {
      this.goToPage(this.productService.currentPage() - 1);
    }
  }

  nextPage(): void {
    if (this.productService.hasNextPage()) {
      this.goToPage(this.productService.currentPage() + 1);
    }
  }

  deleteProduct(id: number): void {
    if (confirm('Are you sure you want to delete this product?')) {
      this.productService.delete(id).subscribe(() => {
        this.loadProducts();
      });
    }
  }
}
