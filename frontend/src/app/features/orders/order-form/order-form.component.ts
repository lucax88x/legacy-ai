import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { OrderService } from '../../../services/order.service';
import { ProductService } from '../../../services/product.service';
import { CreateOrderRequest, UpdateOrderRequest, OrderStatus, OrderStatusLabels, CreateOrderItemRequest } from '../../../models/order.model';

@Component({
  selector: 'app-order-form',
  standalone: true,
  imports: [RouterLink, FormsModule, CurrencyPipe],
  templateUrl: './order-form.component.html',
  styleUrl: './order-form.component.scss'
})
export class OrderFormComponent implements OnInit {
  readonly orderService = inject(OrderService);
  readonly productService = inject(ProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEditMode = signal(false);
  readonly orderId = signal<number | null>(null);
  readonly title = computed(() => this.isEditMode() ? 'Edit Order' : 'Create Order');

  readonly statusOptions = Object.entries(OrderStatusLabels).map(([value, label]) => ({
    value: Number(value) as OrderStatus,
    label
  }));

  readonly formData = signal<{
    customerName: string;
    customerEmail: string;
    customerAddress: string;
    status: OrderStatus;
  }>({
    customerName: '',
    customerEmail: '',
    customerAddress: '',
    status: OrderStatus.Pending
  });

  readonly orderItems = signal<CreateOrderItemRequest[]>([]);

  readonly orderTotal = computed(() =>
    this.orderItems().reduce((sum, item) => sum + (item.quantity * item.unitPrice), 0)
  );

  ngOnInit(): void {
    this.productService.getAll().subscribe();

    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEditMode.set(true);
      this.orderId.set(Number(id));
      this.orderService.getById(Number(id)).subscribe({
        next: (order) => {
          this.formData.set({
            customerName: order.customerName,
            customerEmail: order.customerEmail,
            customerAddress: order.customerAddress,
            status: order.status
          });
          this.orderItems.set(order.orderItems.map(item => ({
            productId: item.productId,
            quantity: item.quantity,
            unitPrice: item.unitPrice
          })));
        }
      });
    }
  }

  updateField(field: 'customerName' | 'customerEmail' | 'customerAddress' | 'status', value: string | OrderStatus): void {
    this.formData.update(data => ({ ...data, [field]: value }));
  }

  addOrderItem(): void {
    this.orderItems.update(items => [
      ...items,
      { productId: 0, quantity: 1, unitPrice: 0 }
    ]);
  }

  removeOrderItem(index: number): void {
    this.orderItems.update(items => items.filter((_, i) => i !== index));
  }

  updateOrderItem(index: number, field: keyof CreateOrderItemRequest, value: number): void {
    this.orderItems.update(items => {
      const updated = [...items];
      updated[index] = { ...updated[index], [field]: value };

      if (field === 'productId') {
        const product = this.productService.products().find(p => p.id === value);
        if (product) {
          updated[index].unitPrice = product.price;
        }
      }

      return updated;
    });
  }

  onSubmit(): void {
    if (this.isEditMode() && this.orderId()) {
      const updateRequest: UpdateOrderRequest = this.formData();
      this.orderService.update(this.orderId()!, updateRequest).subscribe({
        next: () => this.router.navigate(['/orders', this.orderId()])
      });
    } else {
      const createRequest: CreateOrderRequest = {
        ...this.formData(),
        orderItems: this.orderItems()
      };
      this.orderService.create(createRequest).subscribe({
        next: (order) => this.router.navigate(['/orders', order.id])
      });
    }
  }
}
