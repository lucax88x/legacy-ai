import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { OrderService } from '../../../services/order.service';
import { OrderStatusLabels, OrderStatus } from '../../../models/order.model';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss'
})
export class OrderListComponent implements OnInit {
  readonly orderService = inject(OrderService);
  readonly statusLabels = OrderStatusLabels;

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.orderService.getAll(
      this.orderService.currentPage(),
      this.orderService.pageSize()
    ).subscribe();
  }

  goToPage(page: number): void {
    this.orderService.getAll(page, this.orderService.pageSize()).subscribe();
  }

  previousPage(): void {
    if (this.orderService.hasPreviousPage()) {
      this.goToPage(this.orderService.currentPage() - 1);
    }
  }

  nextPage(): void {
    if (this.orderService.hasNextPage()) {
      this.goToPage(this.orderService.currentPage() + 1);
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

  deleteOrder(id: number): void {
    if (confirm('Are you sure you want to delete this order?')) {
      this.orderService.delete(id).subscribe(() => {
        this.loadOrders();
      });
    }
  }
}
