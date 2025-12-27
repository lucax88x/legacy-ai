import http from 'k6/http';
import { check, sleep } from 'k6';
import { Faker } from 'https://jslib.k6.io/k6-faker/0.0.2/index.js';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const faker = new Faker();

const statuses = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

export const options = {
  scenarios: {
    generate_data: {
      executor: 'per-vu-iterations',
      vus: 5,
      iterations: 20,
      maxDuration: '5m',
    },
  },
};

function generateCustomer() {
  const firstName = faker.person.firstName();
  const lastName = faker.person.lastName();

  return {
    customerName: `${firstName} ${lastName}`,
    customerEmail: faker.internet.email({ firstName, lastName }),
    customerAddress: `${faker.location.streetAddress()}, ${faker.location.city()}, ${faker.location.country()}`,
  };
}

function generateOrderItems(productIds) {
  const numItems = randomIntBetween(1, Math.min(4, productIds.length));
  const items = [];
  const usedProducts = new Set();

  for (let i = 0; i < numItems; i++) {
    let productId;
    do {
      productId = productIds[randomIntBetween(0, productIds.length - 1)];
    } while (usedProducts.has(productId) && usedProducts.size < productIds.length);

    usedProducts.add(productId);

    items.push({
      productId: productId,
      quantity: randomIntBetween(1, 5),
      unitPrice: parseFloat(faker.commerce.price({ min: 10, max: 200 })),
    });
  }

  return items;
}

export function setup() {
  const headers = { 'Content-Type': 'application/json' };
  const setupFaker = new Faker();

  // Get existing products to use in orders
  const productsRes = http.get(`${BASE_URL}/api/products`, { headers });

  if (productsRes.status === 200) {
    const products = JSON.parse(productsRes.body);
    if (products.length > 0) {
      return { productIds: products.map(p => p.id) };
    }
  }

  // If no products exist, create some
  console.log('No products found, creating some...');
  const productIds = [];

  for (let i = 0; i < 5; i++) {
    const product = {
      name: setupFaker.commerce.productName(),
      description: setupFaker.commerce.productDescription(),
      category: setupFaker.commerce.department(),
      price: parseFloat(setupFaker.commerce.price({ min: 10, max: 200 })),
      stockQuantity: randomIntBetween(50, 200),
    };

    const res = http.post(`${BASE_URL}/api/products`, JSON.stringify(product), { headers });
    if (res.status === 201) {
      const created = JSON.parse(res.body);
      productIds.push(created.id);
    }
  }

  return { productIds };
}

export default function (data) {
  const headers = { 'Content-Type': 'application/json' };
  const productIds = data.productIds;

  if (!productIds || productIds.length === 0) {
    console.log('No product IDs available for creating orders');
    return;
  }

  // CREATE - POST a new order
  const customer = generateCustomer();
  const orderItems = generateOrderItems(productIds);

  const newOrder = {
    ...customer,
    status: 'Pending',
    orderItems: orderItems,
  };

  const createRes = http.post(
    `${BASE_URL}/api/orders`,
    JSON.stringify(newOrder),
    { headers }
  );

  const createCheck = check(createRes, {
    'create order status is 201': (r) => r.status === 201,
  });

  if (!createCheck) {
    console.log(`Failed to create order: ${createRes.status} - ${createRes.body}`);
    return;
  }

  const createdOrder = JSON.parse(createRes.body);
  const orderId = createdOrder.id;
  console.log(`Created order: ${orderId} for ${customer.customerName}`);

  sleep(randomIntBetween(1, 3));

  // READ - GET the created order
  const getRes = http.get(`${BASE_URL}/api/orders/${orderId}`, { headers });
  check(getRes, {
    'get order status is 200': (r) => r.status === 200,
  });

  sleep(randomIntBetween(1, 2));

  // UPDATE - PUT to update order status (70% chance)
  if (Math.random() > 0.3) {
    const newStatus = statuses[randomIntBetween(1, statuses.length - 1)];

    const updateOrder = {
      customerName: customer.customerName,
      customerEmail: customer.customerEmail,
      customerAddress: customer.customerAddress,
      status: newStatus,
    };

    const updateRes = http.put(
      `${BASE_URL}/api/orders/${orderId}`,
      JSON.stringify(updateOrder),
      { headers }
    );

    check(updateRes, {
      'update order status is 200': (r) => r.status === 200,
    });

    console.log(`Updated order ${orderId} status to: ${newStatus}`);
    sleep(randomIntBetween(1, 2));
  }

  // DELETE - Cancel and remove order (10% chance)
  if (Math.random() > 0.9) {
    const deleteRes = http.del(`${BASE_URL}/api/orders/${orderId}`, null, { headers });
    check(deleteRes, {
      'delete order status is 204 or 200': (r) => r.status === 204 || r.status === 200,
    });
    console.log(`Deleted order: ${orderId}`);
  }

  sleep(randomIntBetween(1, 3));
}
