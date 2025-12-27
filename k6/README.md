# k6 Data Generation Scripts

Scripts for generating fake data to test OpenTelemetry tracing.

Uses [k6-faker](https://jslib.k6.io/k6-faker/) for realistic data generation:
- Product names, descriptions, categories (via `faker.commerce`)
- Customer names, emails, addresses (via `faker.person`, `faker.internet`, `faker.location`)

## Prerequisites

Docker installed on your system.

## Scripts

| Script | Description |
|--------|-------------|
| `products.js` | Creates, reads, updates, and deletes products |
| `orders.js` | Creates, reads, updates, and deletes orders |
| `full-workflow.js` | Complete workflow: creates products, then orders referencing them |

## Usage

Run from the `k6` directory:

```bash
cd k6
```

Run against localhost (default port 5179):

```bash
# Products only
docker run --rm -i --network=host -v $(pwd):/scripts grafana/k6 run /scripts/products.js

# Orders only
docker run --rm -i --network=host -v $(pwd):/scripts grafana/k6 run /scripts/orders.js

# Full workflow (recommended for complete traces)
docker run --rm -i --network=host -v $(pwd):/scripts grafana/k6 run /scripts/full-workflow.js
```

Run against a different URL:

```bash
docker run --rm -i --network=host -v $(pwd):/scripts grafana/k6 run -e BASE_URL=http://localhost:8080 /scripts/full-workflow.js
```

Adjust iterations and virtual users:

```bash
# More data: 10 VUs, 50 iterations each
docker run --rm -i --network=host -v $(pwd):/scripts grafana/k6 run --vus 10 --iterations 50 /scripts/products.js

# Single run for debugging
docker run --rm -i --network=host -v $(pwd):/scripts grafana/k6 run --vus 1 --iterations 1 /scripts/full-workflow.js
```

## Shell Alias (Optional)

Add to your `.bashrc` or `.zshrc` for convenience:

```bash
alias k6='docker run --rm -i --network=host -v $(pwd):/scripts grafana/k6'
```

Then run:

```bash
k6 run /scripts/full-workflow.js
```

## Configuration

Default options in each script:
- **VUs (Virtual Users)**: 3-5
- **Iterations per VU**: 10-20
- **Random delays**: 1-4 seconds between operations

Modify the `options` object in each script to customize behavior.
