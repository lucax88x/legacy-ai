import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../services/product.service';
import { CreateProductRequest, UpdateProductRequest } from '../../../models/product.model';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  readonly productService = inject(ProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEditMode = signal(false);
  readonly productId = signal<number | null>(null);
  readonly title = computed(() => this.isEditMode() ? 'Edit Product' : 'Create Product');

  readonly formData = signal<CreateProductRequest>({
    name: '',
    description: '',
    price: 0,
    stockQuantity: 0,
    category: ''
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEditMode.set(true);
      this.productId.set(Number(id));
      this.productService.getById(Number(id)).subscribe({
        next: (product) => {
          this.formData.set({
            name: product.name,
            description: product.description,
            price: product.price,
            stockQuantity: product.stockQuantity,
            category: product.category
          });
        }
      });
    }
  }

  updateField<K extends keyof CreateProductRequest>(field: K, value: CreateProductRequest[K]): void {
    this.formData.update(data => ({ ...data, [field]: value }));
  }

  onSubmit(): void {
    if (this.isEditMode() && this.productId()) {
      const updateRequest: UpdateProductRequest = this.formData();
      this.productService.update(this.productId()!, updateRequest).subscribe({
        next: () => this.router.navigate(['/products', this.productId()])
      });
    } else {
      this.productService.create(this.formData()).subscribe({
        next: (product) => this.router.navigate(['/products', product.id])
      });
    }
  }
}
