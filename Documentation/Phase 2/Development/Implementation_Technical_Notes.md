## Data Persistence

As defined in the analysis phase of this project, **PostgreSQL** was selected as the primary relational database. To comply with the system's security requirements and ensure data integrity, no in-memory databases are used.

We selected **Aiven** as the managed cloud database provider. Aiven offers a professional hosting environment that allows us to simulate a production-grade scenario where the database is isolated from the application environment.

The selected free tier includes:
* **CPU:** 1 Core
* **RAM:** 1 GB
* **Storage:** 1 GB

This configuration is sufficient for development and integration testing. Furthermore, using an external provider like Aiven ensures that the database resides outside the application's immediate environment, forcing the implementation of secure remote connectivity via **TLS 1.3** and robust secret management, as the connection strings are retrieved directly from our **HashiCorp Vault** instance.