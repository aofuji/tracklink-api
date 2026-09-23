# MCP Spec

Esta spec documenta o comportamento MCP atualmente implementado.

## Endpoint E Transporte

- O servidor MCP MUST estar mapeado em `POST /mcp`.
- O endpoint `/mcp` MUST exigir Bearer JWT por `RequireAuthorization()`.
- Requisicoes sem JWT valido MUST retornar `401 Unauthorized`.
- O servidor MCP usa `ModelContextProtocol.AspNetCore`.
- O transporte configurado MUST ser HTTP.
- O modo de sessao configurado MUST ser stateless (`HttpServerSessionMode.Stateless`).
- Clientes MCP SHOULD enviar `Accept: application/json, text/event-stream`, pois os testes atuais leem respostas em formato SSE com linhas `data:`.
- O metodo JSON-RPC `initialize` autenticado MUST retornar sucesso com `jsonrpc: "2.0"` e `serverInfo`.
- O metodo JSON-RPC `tools/list` autenticado MUST expor as tools implementadas, incluindo `ping`, `get_tracking_status` e `get_tracking_history`.

## UserId E Claims

- Tools que precisam do usuario autenticado MUST obter o usuario da request atual via `IHttpContextAccessor`.
- `get_my_trackings` e `stop_tracking` MUST ler `ClaimTypes.NameIdentifier`.
- Quando aplicavel, a claim de usuario MUST ser convertida para `int`.
- Essas tools MUST NOT aceitar `userId` fornecido pelo cliente.

## Tools

### `ping`

- Parametros: nenhum.
- MUST retornar a string `TrackLink MCP is working!`.

### `who_am_i`

- Parametros: nenhum.
- MUST retornar:
  - `userId`
  - `name`
  - `email`
- Os valores MUST vir das claims `ClaimTypes.NameIdentifier`, `ClaimTypes.Name` e `ClaimTypes.Email`.

### `get_tracking_status`

- Parametros:
  - `token`: token publico do tracking.
- MUST consultar o tracking pelo token publico.
- Quando a consulta nao retornar `Success`, MUST retornar:
  - `status`: nome do estado (`NotFound`, `Inactive` ou `Expired`).
- Quando a consulta retornar `Success`, MUST retornar:
  - `status`: `Success`
  - `tracking.token`
  - `tracking.latitude`
  - `tracking.longitude`
  - `tracking.updatedAt`
  - `tracking.isActive`
  - `tracking.expiresAt`
- A resposta MUST NOT incluir `userId`.

### `get_tracking_history`

- Parametros:
  - `token`: token publico do tracking.
- MUST consultar o historico pelo token publico.
- MUST aplicar as mesmas regras de disponibilidade do tracking publico.
- Quando a consulta nao retornar `Success`, MUST retornar:
  - `status`: nome do estado (`NotFound`, `Inactive` ou `Expired`).
- Quando a consulta retornar `Success`, MUST retornar:
  - `status`: `Success`
  - `locations`: lista de itens com `latitude`, `longitude` e `recordedAt`.
- A ordem das localizacoes MUST ser cronologica crescente por `recordedAt`.

### `get_my_trackings`

- Parametros: nenhum.
- MUST obter o usuario autenticado pela claim `ClaimTypes.NameIdentifier`.
- Quando o usuario autenticado nao puder ser identificado, MUST retornar:
  - `success`: `false`
  - `error`: `Authenticated user could not be identified.`
- Quando o usuario for identificado, MUST retornar:
  - `success`: `true`
  - `trackings`: lista dos trackings pertencentes ao usuario autenticado.
- Cada item de `trackings` MUST conter `token`, `latitude`, `longitude`, `updatedAt`, `isActive` e `expiresAt`.
- A lista MUST respeitar a ordenacao do servico: `updatedAt` decrescente.
- A tool MUST NOT retornar trackings de outro usuario.

### `stop_tracking`

- Parametros:
  - `token`: token publico do tracking.
- MUST obter o usuario autenticado pela claim `ClaimTypes.NameIdentifier`.
- Quando o usuario autenticado nao puder ser identificado, MUST retornar:
  - `success`: `false`
  - `error`: `Authenticated user could not be identified.`
- MUST encerrar somente tracking pertencente ao usuario autenticado.
- Quando o tracking nao existir para o usuario autenticado, MUST retornar:
  - `success`: `false`
  - `error`: `Tracking session was not found.`
- Esse comportamento tambem impede que um usuario encerre tracking de outro usuario.
- Quando o encerramento for bem-sucedido, MUST:
  - definir a sessao como inativa pelo mesmo fluxo usado pelo servico de tracking;
  - emitir `TrackingEnded` via SignalR para o grupo do token;
  - retornar `success: true`.
