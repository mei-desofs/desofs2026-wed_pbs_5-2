# Relatório de Análise Dinâmica de Segurança (DAST) - OWASP ZAP

## 1. Introdução

No âmbito da **Phase 2 - Sprint 1**, foi realizada a análise dinâmica de segurança (DAST) à aplicação utilizando a ferramenta **OWASP ZAP**.

Os testes foram efetuados sobre a versão inicial da nossa aplicação.

---

## 2. Objetivo

- Identificar vulnerabilidades em tempo de execução
- Validar a exposição da API
- Produzir evidências para o ASVS e para o Deliverable principal do Sprint 1

---

## 3. Configuração e Execução

### Ambiente de Teste
- **URL Alvo:** `http://host.docker.internal:5139`
- **Tipo de Aplicação:** ASP.NET Core Web API
- **Ferramenta:** OWASP ZAP
- **Modo de Scan:** Baseline Scan + Active Scan

### Comandos Utilizados

#### Baseline Scan (Passivo)
```bash
docker run -t \
  -v $(pwd)/zap-reports:/zap/wrk \
  ghcr.io/zaproxy/zaproxy:stable zap-baseline.py \
  -t http://host.docker.internal:5139 \
  -r zap-baseline-report.html
```

#### Active Scan (Ativo)
```bash
docker run -t \
  -v $(pwd)/zap-reports:/zap/wrk \
  ghcr.io/zaproxy/zaproxy:stable zap-full-scan.py \
  -t http://host.docker.internal:5139 \
  -r zap-active-report.html
```

---

## 4. Resumo de Resultados

### Distribuição de Alertas

| Nível de Risco | Baseline | Active | Total |
|---|---|---|---|
| High | 0 | 0 | 0 |
| Medium | 0 | 1 | 1 |
| Low | 0 | 0 | 0 |
| Informational | 1 | 1 | 2 |

### Resumo Geral

- Não foram identificadas vulnerabilidades críticas ou de alto risco.
- Foi identificado um alerta Medium relacionado com utilização exclusiva de HTTP.
- Foram identificados alertas informativos relacionados com políticas de cache.

---

## 5. Alertas Encontrados

### 5.1 Medium - HTTP Only Site

- **Descrição:** A aplicação está a ser servida apenas em HTTP, sem suporte a HTTPS.
- **URL:** `http://host.docker.internal:5139`
- **Impacto:** Dados transmitidos em claro (sem encriptação).
- **Estado:** Conhecido e aceite temporariamente para ambiente de desenvolvimento.
- **Plano de Mitigação:** Implementação de HTTPS (TLS/SSL) planeada para o Sprint 2 da phase 2.

---

### 5.2 Informational - Storable and Cacheable Content

- **Descrição:** Algumas respostas não possuem headers adequados para controlo de cache.
- **Número de ocorrências:** 1 no Baseline Scan e 1 no Active Scan.
- **URLs afetadas:** `/`, `/robots.txt`, entre outras.
- **Recomendação:** Adicionar os seguintes headers nas respostas da API:

```http
Cache-Control: no-cache, no-store, must-revalidate, private
Pragma: no-cache
Expires: 0
```

---

## 6. Falsos Positivos e Observações

- Não foram identificados falsos positivos de risco alto ou médio.
- O alerta de HTTP Only é válido, mas considerado aceitável nesta fase inicial do projeto.
- Alguns endpoints retornam respostas 4xx de forma intencional, comportamento esperado em APIs stub.
- A reduzida superfície de ataque disponível limita naturalmente o número de findings do Active Scan.

---

## 7. Vulnerabilidades Reportadas à Equipa

Foram reportadas as seguintes recomendações:

1. Implementação de HTTPS.
2. Configuração de headers de cache e segurança.
3. Implementação futura de:
   - Content Security Policy (CSP)
   - HSTS
   - X-Content-Type-Options
   - X-Frame-Options

---

## 8. Evidências

- [Baseline Report](./zap-baseline-report.pdf)
- [Active Report](./zap-active-report.pdf)

---

## 9. Conclusão

A aplicação apresenta um nível de segurança adequado para o estado atual de desenvolvimento (*stub implementation*).

Não foram detetadas vulnerabilidades High ou críticas durante os scans realizados com OWASP ZAP.

Os alertas encontrados estão relacionados principalmente com configurações de ambiente de desenvolvimento, sendo esperada a sua mitigação em futuras iterações do projeto.
