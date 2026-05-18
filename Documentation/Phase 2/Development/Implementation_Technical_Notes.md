## Persistência de Dados

Conforme definido na fase de análise deste projeto, o **PostgreSQL** foi selecionado como base de dados relacional primária. Para cumprir os requisitos de segurança do sistema e garantir a integridade dos dados, não são utilizadas bases de dados em memória.

Selecionámos a **Aiven** como fornecedor de base de dados na cloud gerida. A Aiven oferece um ambiente de alojamento profissional que nos permite simular um cenário equiparável a produção, em que a base de dados está isolada do ambiente aplicacional.

O tier gratuito escolhido inclui:
* **CPU:** 1 Core
* **RAM:** 1 GB
* **Armazenamento:** 1 GB

Esta configuração é suficiente para desenvolvimento e testes de integração. Além disso, usar um fornecedor externo como a Aiven garante que a base de dados reside fora do ambiente imediato da aplicação, forçando a implementação de conectividade remota segura via **TLS 1.3** e uma gestão robusta de segredos, dado que as connection strings são obtidas diretamente da nossa instância do **HashiCorp Vault**.
