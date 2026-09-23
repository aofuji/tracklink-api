# Realtime Spec

Esta spec documenta o comportamento SignalR atualmente implementado.

## Hub

- A aplicacao MUST registrar SignalR.
- O hub de tracking MUST estar disponivel em `/hubs/tracking`.
- O hub atual nao exige autenticacao propria para conexao ou entrada em grupo.

## Grupos Por Tracking Token

- Clientes entram em grupos chamando o metodo de hub `JoinTracking(token)`.
- O nome do grupo MUST ser exatamente o token publico recebido.
- `JoinTracking(token)` MUST consultar a sessao pelo token.
- `JoinTracking(token)` MUST aceitar a entrada no grupo somente quando a sessao existir, estiver ativa e nao estiver expirada.
- Se a sessao nao estiver disponivel, `JoinTracking(token)` MUST lancar `HubException` com a mensagem `Tracking session is not available.`

## Evento `LocationUpdated`

- O evento `LocationUpdated` MUST ser emitido apos uma atualizacao HTTP bem-sucedida em `PUT /api/tracking/{token}`.
- O evento MUST ser enviado ao grupo cujo nome e o token atualizado.
- O payload MUST ser o mesmo formato de resposta da atualizacao HTTP:
  - `token`
  - `latitude`
  - `longitude`
  - `updatedAt`
  - `isActive`
  - `expiresAt`
- O evento MUST NOT ser emitido quando a atualizacao falhar por `NotFound`, `Inactive`, `Expired` ou usuario nao autenticado.

## Evento `TrackingEnded`

- O evento `TrackingEnded` MUST ser emitido apos encerramento HTTP bem-sucedido em `DELETE /api/tracking/{token}`.
- O evento `TrackingEnded` MUST ser emitido apos a tool MCP `stop_tracking` encerrar uma sessao com sucesso.
- O evento MUST ser enviado ao grupo cujo nome e o token encerrado.
- O evento atual nao envia payload.
- O evento MUST NOT ser emitido quando o encerramento falhar por token inexistente, usuario nao autenticado ou usuario que nao e owner.
