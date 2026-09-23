# Tracking Spec

Esta spec documenta o comportamento de tracking atualmente implementado.

## Criacao

### `POST /api/tracking`

- MUST exigir Bearer JWT.
- MUST obter o usuario pela claim `ClaimTypes.NameIdentifier`.
- MUST retornar `401 Unauthorized` se a claim de usuario autenticado nao puder ser convertida para `int`.
- MUST receber `latitude` e `longitude`.
- MUST criar uma sessao de tracking pertencente ao usuario autenticado.
- MUST gerar um token publico com `Guid.NewGuid().ToString("N")`.
- O token publico MUST ser unico no banco.
- MUST definir `isActive` como `true`.
- MUST definir `updatedAt` com o instante UTC da criacao.
- MUST definir `expiresAt` para 24 horas apos a criacao.
- MUST criar uma entrada inicial no historico com a latitude/longitude iniciais e `recordedAt` igual ao instante UTC da criacao.
- MUST retornar `201 Created` com `token`, `latitude`, `longitude`, `updatedAt`, `isActive` e `expiresAt`.

## Acesso Publico Por Token

### `GET /api/tracking/{token}`

- MUST ser publico.
- MUST retornar `200 OK` com `token`, `latitude`, `longitude`, `updatedAt`, `isActive` e `expiresAt` quando a sessao existir, estiver ativa e nao estiver expirada.
- MUST retornar `404 Not Found` quando o token nao existir.
- MUST retornar `409 Conflict` com `message: "Tracking session is inactive."` quando a sessao estiver inativa.
- MUST retornar `410 Gone` com `message: "Tracking session has expired."` quando a sessao estiver expirada.
- A resposta publica MUST NOT incluir `userId`.

### `GET /api/tracking/{token}/history`

- MUST ser publico.
- MUST aplicar as mesmas regras de disponibilidade de `GET /api/tracking/{token}`.
- MUST retornar `200 OK` com uma lista de localizacoes quando a sessao existir, estiver ativa e nao estiver expirada.
- Cada item do historico MUST conter `latitude`, `longitude` e `recordedAt`.
- O historico MUST ser retornado em ordem cronologica crescente por `recordedAt`.

## Atualizacao

### `PUT /api/tracking/{token}`

- MUST exigir Bearer JWT.
- MUST obter o usuario pela claim `ClaimTypes.NameIdentifier`.
- MUST retornar `401 Unauthorized` se a claim de usuario autenticado nao puder ser convertida para `int`.
- MUST permitir atualizacao apenas quando o token pertence ao usuario autenticado.
- MUST retornar `404 Not Found` quando o token nao existir para o usuario autenticado. Isso tambem impede que um usuario modifique tracking de outro usuario.
- MUST retornar `409 Conflict` com `message: "Tracking session is inactive."` quando a sessao do usuario estiver inativa.
- MUST retornar `409 Conflict` com `message: "Tracking session has expired."` quando a sessao do usuario estiver expirada.
- Quando bem-sucedida, MUST atualizar `latitude`, `longitude` e `updatedAt`.
- Quando bem-sucedida, MUST criar nova entrada no historico com `latitude`, `longitude`, `recordedAt` e o tracking correspondente.
- Quando bem-sucedida, MUST retornar `200 OK` com `token`, `latitude`, `longitude`, `updatedAt`, `isActive` e `expiresAt`.
- Quando bem-sucedida, MUST emitir o evento SignalR `LocationUpdated` para o grupo do token com a resposta atualizada.

## Encerramento

### `DELETE /api/tracking/{token}`

- MUST exigir Bearer JWT.
- MUST obter o usuario pela claim `ClaimTypes.NameIdentifier`.
- MUST retornar `401 Unauthorized` se a claim de usuario autenticado nao puder ser convertida para `int`.
- MUST permitir encerramento apenas quando o token pertence ao usuario autenticado.
- MUST retornar `404 Not Found` quando o token nao existir para o usuario autenticado. Isso tambem impede que um usuario encerre tracking de outro usuario.
- Quando bem-sucedido, MUST definir `isActive` como `false` e atualizar `updatedAt`.
- Quando bem-sucedido, MUST emitir o evento SignalR `TrackingEnded` para o grupo do token.
- Quando bem-sucedido, MUST retornar `204 No Content`.
- Encerrar uma sessao nao remove a sessao nem seu historico do banco.

## Listagem Do Usuario

### `GET /api/tracking/my`

- MUST exigir Bearer JWT.
- MUST obter o usuario pela claim `ClaimTypes.NameIdentifier`.
- MUST retornar `401 Unauthorized` se a claim de usuario autenticado nao puder ser convertida para `int`.
- MUST retornar somente trackings cujo `userId` corresponde ao usuario autenticado.
- MUST ordenar os trackings por `updatedAt` decrescente.
- MUST retornar uma lista de itens com `token`, `latitude`, `longitude`, `updatedAt`, `isActive` e `expiresAt`.

## Estados Existentes

As consultas de tracking usam os estados:

- `Success`
- `NotFound`
- `Inactive`
- `Expired`

As atualizacoes de tracking usam os estados:

- `Success`
- `NotFound`
- `Inactive`
- `Expired`

No comportamento HTTP atual, tracking expirado retorna `410 Gone` em consultas publicas e `409 Conflict` em atualizacoes autenticadas.
