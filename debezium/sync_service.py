"""
Debezium CDC to Qdrant Sync Service

This service:
1. Consumes change events from Kafka (produced by Debezium)
2. Generates embeddings from the data using OpenAI
3. Upserts vectors to Qdrant for semantic search
"""

import os
import json
import time
import logging
from kafka import KafkaConsumer
from qdrant_client import QdrantClient
from qdrant_client.http import models
from openai import OpenAI

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Configuration from environment
KAFKA_BOOTSTRAP_SERVERS = os.getenv('KAFKA_BOOTSTRAP_SERVERS', 'kafka:29092')
QDRANT_HOST = os.getenv('QDRANT_HOST', 'qdrant')
QDRANT_PORT = int(os.getenv('QDRANT_PORT', '6333'))

# Kafka topics (Debezium format: {topic.prefix}.{schema}.{table})
TOPICS = [
    'legacy.public.Products',
    'legacy.public.Orders',
    'legacy.public.OrderItems'
]

# Qdrant collection names
COLLECTIONS = {
    'legacy.public.Products': 'products',
    'legacy.public.Orders': 'orders',
    'legacy.public.OrderItems': 'order_items'
}

# OpenAI embedding configuration
OPENAI_API_KEY = os.getenv('OPENAI_API_KEY')
EMBEDDING_MODEL = 'text-embedding-3-small'
VECTOR_SIZE = 1536  # Output dimension of text-embedding-3-small


def wait_for_kafka(max_retries=30, retry_interval=5):
    """Wait for Kafka to be available."""
    for i in range(max_retries):
        try:
            consumer = KafkaConsumer(
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                consumer_timeout_ms=1000
            )
            consumer.close()
            logger.info("Kafka is available!")
            return True
        except Exception as e:
            logger.info(f"Waiting for Kafka... ({i+1}/{max_retries})")
            time.sleep(retry_interval)
    raise Exception("Kafka not available after max retries")


def wait_for_qdrant(client, max_retries=30, retry_interval=5):
    """Wait for Qdrant to be available."""
    for i in range(max_retries):
        try:
            client.get_collections()
            logger.info("Qdrant is available!")
            return True
        except Exception as e:
            logger.info(f"Waiting for Qdrant... ({i+1}/{max_retries})")
            time.sleep(retry_interval)
    raise Exception("Qdrant not available after max retries")


def create_collections(client):
    """Create Qdrant collections if they don't exist."""
    existing = {c.name for c in client.get_collections().collections}

    for collection_name in COLLECTIONS.values():
        if collection_name not in existing:
            client.create_collection(
                collection_name=collection_name,
                vectors_config=models.VectorParams(
                    size=VECTOR_SIZE,
                    distance=models.Distance.COSINE
                )
            )
            logger.info(f"Created collection: {collection_name}")
        else:
            logger.info(f"Collection exists: {collection_name}")


def create_product_text(data):
    """Create searchable text from product data."""
    return f"""
    Product: {data.get('Name', '')}
    Description: {data.get('Description', '')}
    Category: {data.get('Category', '')}
    Price: ${data.get('Price', 0)}
    Stock: {data.get('StockQuantity', 0)} units
    """.strip()


def create_order_text(data):
    """Create searchable text from order data."""
    status_map = {0: 'Pending', 1: 'Processing', 2: 'Shipped', 3: 'Delivered', 4: 'Cancelled'}
    status = status_map.get(data.get('Status', 0), 'Unknown')
    return f"""
    Order for: {data.get('CustomerName', '')}
    Email: {data.get('CustomerEmail', '')}
    Address: {data.get('CustomerAddress', '')}
    Status: {status}
    Total: ${data.get('TotalAmount', 0)}
    """.strip()


def create_order_item_text(data):
    """Create searchable text from order item data."""
    return f"""
    Order Item: Order #{data.get('OrderId', '')} - Product #{data.get('ProductId', '')}
    Quantity: {data.get('Quantity', 0)}
    Unit Price: ${data.get('UnitPrice', 0)}
    Total: ${data.get('TotalPrice', 0)}
    """.strip()


def get_embedding(openai_client, text):
    """Generate embedding using OpenAI API."""
    response = openai_client.embeddings.create(
        input=text,
        model=EMBEDDING_MODEL
    )
    return response.data[0].embedding


def process_event(event, topic, openai_client, qdrant_client):
    """Process a single CDC event."""
    try:
        # Debezium event structure
        payload = event.get('payload', {})
        operation = payload.get('op')  # c=create, u=update, d=delete, r=read (snapshot)

        collection_name = COLLECTIONS.get(topic)
        if not collection_name:
            logger.warning(f"Unknown topic: {topic}")
            return

        # Handle delete
        if operation == 'd':
            before = payload.get('before', {})
            record_id = before.get('Id')
            if record_id:
                qdrant_client.delete(
                    collection_name=collection_name,
                    points_selector=models.PointIdsList(points=[record_id])
                )
                logger.info(f"Deleted {collection_name}/{record_id}")
            return

        # Handle create/update/snapshot
        after = payload.get('after', {})
        if not after:
            return

        record_id = after.get('Id')
        if not record_id:
            return

        # Generate text for embedding based on table type
        if 'Products' in topic:
            text = create_product_text(after)
        elif 'Orders' in topic and 'Items' not in topic:
            text = create_order_text(after)
        else:
            text = create_order_item_text(after)

        # Generate embedding using OpenAI
        embedding = get_embedding(openai_client, text)

        # Upsert to Qdrant
        qdrant_client.upsert(
            collection_name=collection_name,
            points=[
                models.PointStruct(
                    id=record_id,
                    vector=embedding,
                    payload=after  # Store original data as payload
                )
            ]
        )

        op_name = {'c': 'Created', 'u': 'Updated', 'r': 'Snapshot'}.get(operation, operation)
        logger.info(f"{op_name} {collection_name}/{record_id}")

    except Exception as e:
        logger.error(f"Error processing event: {e}")


def main():
    logger.info("Starting Qdrant Sync Service...")

    if not OPENAI_API_KEY:
        raise ValueError("OPENAI_API_KEY environment variable is required")

    # Wait for dependencies
    wait_for_kafka()

    qdrant_client = QdrantClient(host=QDRANT_HOST, port=QDRANT_PORT)
    wait_for_qdrant(qdrant_client)

    # Initialize collections
    create_collections(qdrant_client)

    # Initialize OpenAI client
    logger.info(f"Using OpenAI embedding model: {EMBEDDING_MODEL}")
    openai_client = OpenAI(api_key=OPENAI_API_KEY)
    logger.info("OpenAI client initialized!")

    # Create Kafka consumer
    consumer = KafkaConsumer(
        *TOPICS,
        bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
        auto_offset_reset='earliest',  # Process all historical events
        enable_auto_commit=True,
        group_id='qdrant-sync-group',
        value_deserializer=lambda x: json.loads(x.decode('utf-8')) if x else None
    )

    logger.info(f"Subscribed to topics: {TOPICS}")
    logger.info("Waiting for CDC events...")

    # Process events
    for message in consumer:
        if message.value:
            process_event(message.value, message.topic, openai_client, qdrant_client)


if __name__ == '__main__':
    main()
