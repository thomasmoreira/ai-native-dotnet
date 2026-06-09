# distributed-consistency-lab

Reference lab for distributed data consistency over RabbitMQ with PostgreSQL and EF Core.

## Transactional Outbox
Writes the domain change and an outbox message in the SAME database transaction. A background
dispatcher polls unsent rows (FOR UPDATE SKIP LOCKED) and publishes to RabbitMQ with publisher
confirms. This guarantees at-least-once delivery without a distributed transaction.

## Inbox (idempotent consumer)
Each consumer records processed message ids in an inbox table; re-deliveries are ignored,
giving an exactly-once effect on top of at-least-once transport.

## Saga
A multi-step business process (order → stock → payment) coordinated with compensation: if a
later step fails, earlier steps are compensated (e.g. stock restored), keeping the system
eventually consistent. Both orchestration and choreography styles are shown.
