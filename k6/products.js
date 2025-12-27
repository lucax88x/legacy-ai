import http from 'k6/http';
import { check, sleep } from 'k6';
import { Faker } from 'https://jslib.k6.io/k6-faker/0.0.2/index.js';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const faker = new Faker();

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

function generateProduct() {
  return {
    name: faker.commerce.productName(),
    description: faker.commerce.productDescription(),
    category: faker.commerce.department(),
    price: parseFloat(faker.commerce.price({ min: 10, max: 500 })),
    stockQuantity: randomIntBetween(10, 500),
  };
}

export default function () {
  const headers = { 'Content-Type': 'application/json' };

  // CREATE - POST a new product
  const newProduct = generateProduct();
  const createRes = http.post(
    `${BASE_URL}/api/products`,
    JSON.stringify(newProduct),
    { headers }
  );

  const createCheck = check(createRes, {
    'create product status is 201': (r) => r.status === 201,
  });

  if (!createCheck) {
    console.log(`Failed to create product: ${createRes.status} - ${createRes.body}`);
    return;
  }

  const createdProduct = JSON.parse(createRes.body);
  const productId = createdProduct.id;
  console.log(`Created product: ${productId} - ${newProduct.name}`);

  sleep(randomIntBetween(1, 3));

  // READ - GET the created product
  const getRes = http.get(`${BASE_URL}/api/products/${productId}`, { headers });
  check(getRes, {
    'get product status is 200': (r) => r.status === 200,
  });

  sleep(randomIntBetween(1, 2));

  // UPDATE - PUT to update the product (50% chance)
  if (Math.random() > 0.5) {
    const updatedProduct = {
      name: `${faker.commerce.productAdjective()} ${faker.commerce.product()}`,
      description: faker.commerce.productDescription(),
      category: faker.commerce.department(),
      price: parseFloat(faker.commerce.price({ min: 20, max: 600 })),
      stockQuantity: randomIntBetween(50, 300),
    };

    const updateRes = http.put(
      `${BASE_URL}/api/products/${productId}`,
      JSON.stringify(updatedProduct),
      { headers }
    );

    check(updateRes, {
      'update product status is 200': (r) => r.status === 200,
    });

    console.log(`Updated product: ${productId}`);
    sleep(randomIntBetween(1, 2));
  }

  // DELETE - Remove product (20% chance)
  if (Math.random() > 0.8) {
    const deleteRes = http.del(`${BASE_URL}/api/products/${productId}`, null, { headers });
    check(deleteRes, {
      'delete product status is 204 or 200': (r) => r.status === 204 || r.status === 200,
    });
    console.log(`Deleted product: ${productId}`);
  }

  sleep(randomIntBetween(1, 3));
}
