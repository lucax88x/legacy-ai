import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Faker } from 'https://jslib.k6.io/k6-faker/0.0.2/index.js';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5179';
const faker = new Faker();

const statuses = ['Pending', 'Processing', 'Shipped', 'Delivered'];

export const options = {
  scenarios: {
    full_workflow: {
      executor: 'per-vu-iterations',
      vus: 3,
      iterations: 10,
      maxDuration: '10m',
    },
  },
};

function generateProduct() {
  return {
    name: faker.commerce.productName(),
    description: faker.commerce.productDescription(),
    category: faker.commerce.department(),
    price: parseFloat(faker.commerce.price({ min: 10, max: 500 })),
    stockQuantity: randomIntBetween(10, 500),
  };
}

function generateCustomer() {
  const firstName = faker.person.firstName();
  const lastName = faker.person.lastName();

  return {
    customerName: `${firstName} ${lastName}`,
    customerEmail: faker.internet.email({ firstName, lastName }),
    customerAddress: `${faker.location.streetAddress()}, ${faker.location.city()}, ${faker.location.country()}`,
  };
}

export default function () {
  const headers = { 'Content-Type': 'application/json' };
  const createdProductIds = [];

  // PHASE 1: Create multiple products
  group('Create Products', function () {
    const numProducts = randomIntBetween(2, 5);

    for (let i = 0; i < numProducts; i++) {
      const product = generateProduct();

      const res = http.post(
        `${BASE_URL}/api/products`,
        JSON.stringify(product),
        { headers }
      );

      if (check(res, { 'product created': (r) => r.status === 201 })) {
        const created = JSON.parse(res.body);
        createdProductIds.push({ id: created.id, price: product.price });
        console.log(`[Products] Created: ${created.id} - ${product.name}`);
      }

      sleep(randomIntBetween(1, 2));
    }
  });

  sleep(randomIntBetween(2, 4));

  // PHASE 2: List all products
  group('List Products', function () {
    const res = http.get(`${BASE_URL}/api/products`, { headers });
    check(res, { 'products listed': (r) => r.status === 200 });

    if (res.status === 200) {
      const products = JSON.parse(res.body);
      console.log(`[Products] Total in database: ${products.length}`);
    }
  });

  sleep(randomIntBetween(1, 2));

  // PHASE 3: Update some products
  group('Update Products', function () {
    for (const prod of createdProductIds) {
      if (Math.random() > 0.5) {
        const updatedProduct = generateProduct();

        const res = http.put(
          `${BASE_URL}/api/products/${prod.id}`,
          JSON.stringify(updatedProduct),
          { headers }
        );

        check(res, { 'product updated': (r) => r.status === 200 });
        console.log(`[Products] Updated: ${prod.id}`);
        sleep(randomIntBetween(1, 2));
      }
    }
  });

  sleep(randomIntBetween(2, 3));

  // PHASE 4: Create orders using created products
  group('Create Orders', function () {
    const numOrders = randomIntBetween(1, 3);

    for (let i = 0; i < numOrders; i++) {
      const customer = generateCustomer();
      const numItems = randomIntBetween(1, Math.min(3, createdProductIds.length));
      const orderItems = [];

      for (let j = 0; j < numItems; j++) {
        const prod = createdProductIds[j % createdProductIds.length];
        orderItems.push({
          productId: prod.id,
          quantity: randomIntBetween(1, 5),
          unitPrice: prod.price,
        });
      }

      const order = {
        ...customer,
        status: 'Pending',
        orderItems: orderItems,
      };

      const res = http.post(
        `${BASE_URL}/api/orders`,
        JSON.stringify(order),
        { headers }
      );

      if (check(res, { 'order created': (r) => r.status === 201 })) {
        const created = JSON.parse(res.body);
        console.log(`[Orders] Created: ${created.id} for ${customer.customerName}`);

        // Update order status through lifecycle
        sleep(randomIntBetween(1, 2));

        for (const status of ['Processing', 'Shipped']) {
          if (Math.random() > 0.3) {
            const updateOrder = {
              ...customer,
              status: status,
            };

            const updateRes = http.put(
              `${BASE_URL}/api/orders/${created.id}`,
              JSON.stringify(updateOrder),
              { headers }
            );

            check(updateRes, { 'order status updated': (r) => r.status === 200 });
            console.log(`[Orders] Updated ${created.id} -> ${status}`);
            sleep(randomIntBetween(1, 2));
          }
        }
      }

      sleep(randomIntBetween(1, 3));
    }
  });

  sleep(randomIntBetween(2, 3));

  // PHASE 5: List all orders
  group('List Orders', function () {
    const res = http.get(`${BASE_URL}/api/orders`, { headers });
    check(res, { 'orders listed': (r) => r.status === 200 });

    if (res.status === 200) {
      const orders = JSON.parse(res.body);
      console.log(`[Orders] Total in database: ${orders.length}`);
    }
  });

  sleep(randomIntBetween(1, 2));

  // PHASE 6: Cleanup - Delete some products (optional, low probability)
  group('Cleanup', function () {
    for (const prod of createdProductIds) {
      if (Math.random() > 0.85) {
        const res = http.del(`${BASE_URL}/api/products/${prod.id}`, null, { headers });
        check(res, { 'product deleted': (r) => r.status === 204 || r.status === 200 });
        console.log(`[Cleanup] Deleted product: ${prod.id}`);
        sleep(1);
      }
    }
  });

  sleep(randomIntBetween(2, 4));
}
