import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { OrderService } from '../../../services/order.service';
import { OrderStatusLabels, OrderStatus } from '../../../models/order.model';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss'
})
export class OrderDetailComponent implements OnInit {
  readonly orderService = inject(OrderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly statusLabels = OrderStatusLabels;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.orderService.getById(id).subscribe();
    }
  }

  getStatusLabel(status: OrderStatus): string {
    return this.statusLabels[status] || 'Unknown';
  }

  getStatusClass(status: OrderStatus): string {
    const classes: Record<OrderStatus, string> = {
      [OrderStatus.Pending]: 'status-pending',
      [OrderStatus.Processing]: 'status-processing',
      [OrderStatus.Shipped]: 'status-shipped',
      [OrderStatus.Delivered]: 'status-delivered',
      [OrderStatus.Cancelled]: 'status-cancelled'
    };
    return classes[status] || '';
  }

  deleteOrder(): void {
    const order = this.orderService.selectedOrder();
    if (order && confirm('Are you sure you want to delete this order?')) {
      this.orderService.delete(order.id).subscribe({
        next: () => this.router.navigate(['/orders'])
      });
    }
  }
}
