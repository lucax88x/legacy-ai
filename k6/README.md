# k6 Data Generation Scripts

Scripts for generating fake data to test OpenTelemetry tracing.

Uses [k6-faker](https://jslib.k6.io/k6-faker/) for realistic data generation:
- Product names, descriptions, categories (via `faker.commerce`)
- Customer names, emails, addresses (via `faker.person`, `faker.internet`, `faker.location`)

## Prerequisites

Install k6: https://k6.io/docs/get-started/installation/

```bash
# macOS
brew install k6

# Ubuntu/Debian
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6

# Windows
winget install k6
```

## Scripts

| Script | Description |
|--------|-------------|
| `products.js` | Creates, reads, updates, and deletes products |
| `orders.js` | Creates, reads, updates, and deletes orders |
| `full-workflow.js` | Complete workflow: creates products, then orders referencing them |

## Usage

Run against localhost (default port 5000):

```bash
# Products only
k6 run products.js

# Orders only
k6 run orders.js

# Full workflow (recommended for complete traces)
k6 run full-workflow.js
```

Run against a different URL:

```bash
k6 run -e BASE_URL=http://localhost:8080 full-workflow.js
```

Adjust iterations and virtual users:

```bash
# More data: 10 VUs, 50 iterations each
k6 run --vus 10 --iterations 50 products.js

# Single run for debugging
k6 run --vus 1 --iterations 1 full-workflow.js
```

## Configuration

Default options in each script:
- **VUs (Virtual Users)**: 3-5
- **Iterations per VU**: 10-20
- **Random delays**: 1-4 seconds between operations

Modify the `options` object in each script to customize behavior.
