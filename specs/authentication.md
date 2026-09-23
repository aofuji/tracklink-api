# Authentication Spec

Esta spec documenta o comportamento esperado de autenticacao.

## Endpoints

### `POST /api/auth/register`

- MUST ser publico.
- MUST receber `name`, `email` e `password`.
- MUST criar um usuario quando o email ainda nao estiver registrado.
- MUST armazenar a senha como hash usando o mecanismo de password hashing do ASP.NET Core Identity.
- MUST retornar `201 Created` com `id`, `name`, `email` e `createdAt` quando o registro for criado.
- MUST retornar `409 Conflict` com `message: "Email is already registered."` quando o email ja existir.
- O email possui indice unico no banco.

### `POST /api/auth/login`

- MUST ser publico.
- MUST receber `email` e `password`.
- MUST validar a senha contra o hash armazenado.
- MUST retornar `200 OK` com `accessToken` quando as credenciais forem validas.
- MUST NOT retornar o refresh token no corpo da resposta.
- MUST enviar o refresh token por cookie `HttpOnly` quando as credenciais forem validas.
- MUST retornar `401 Unauthorized` com `message: "Invalid email or password."` quando o usuario nao existir ou a senha for invalida.

### `POST /api/auth/refresh`

- MUST ser publico.
- MUST obter o refresh token atraves do cookie `HttpOnly`.
- MUST gerar um novo access token e um novo refresh token quando o refresh token obtido do cookie existir, nao estiver revogado e nao estiver expirado.
- MUST rotacionar o refresh token: o token antigo MUST ser revogado e um novo registro de refresh token MUST ser criado.
- MUST retornar `200 OK` com `accessToken` quando a rotacao for bem-sucedida.
- MUST NOT retornar o refresh token no corpo da resposta.
- MUST substituir o refresh token anterior no cookie pelo novo refresh token quando a rotacao for bem-sucedida.
- MUST retornar `401 Unauthorized` com `message: "Invalid or expired refresh token."` quando o refresh token for invalido, revogado ou expirado.
- Um refresh token ja rotacionado MUST NOT poder ser reutilizado.

### `POST /api/auth/logout`

- MUST ser publico.
- MUST obter o refresh token atraves do cookie `HttpOnly`.
- MUST revogar o refresh token correspondente quando ele existir e ainda nao estiver revogado.
- MUST expirar/remover o cookie do refresh token quando o logout for bem-sucedido.
- MUST retornar `204 No Content` quando a revogacao for bem-sucedida.
- MUST retornar `401 Unauthorized` com `message: "Invalid refresh token."` quando o refresh token nao existir ou ja estiver revogado.
- Um refresh token revogado por logout MUST NOT poder ser usado em `/api/auth/refresh`.

### `GET /api/auth/me`

- MUST exigir Bearer JWT.
- MUST retornar `200 OK` com `id`, `name` e `email` extraidos das claims do JWT autenticado.
- MUST retornar `401 Unauthorized` quando chamado sem access token valido.

## JWT Access Token

- O access token MUST ser um JWT Bearer assinado com HMAC SHA-256.
- A validacao JWT MUST validar issuer, audience, lifetime e signing key.
- `Jwt:Key` MUST estar configurado; a aplicacao falha ao gerar/configurar tokens sem essa chave.
- `Jwt:Issuer` e `Jwt:Audience` sao usados como issuer e audience validos.
- `Jwt:ExpirationMinutes` define a duracao do access token; quando ausente, a geracao usa `15` minutos.

## Claims Utilizadas

O access token gerado MUST conter:

- `ClaimTypes.NameIdentifier`: `user.Id` como string.
- `ClaimTypes.Name`: `user.Name`.
- `ClaimTypes.Email`: `user.Email`.

As rotas e tools que precisam identificar o usuario autenticado leem `ClaimTypes.NameIdentifier` e esperam que ele possa ser convertido para `int`.

## Refresh Token

- O refresh token MUST ser gerado a partir de 64 bytes aleatorios e codificado em Base64.
- O refresh token MUST ser entregue ao cliente somente atraves de cookie `HttpOnly`.
- O refresh token MUST NOT ser disponibilizado ao JavaScript atraves do response body.
- O valor bruto do refresh token MUST NOT ser armazenado no banco.
- O armazenamento MUST usar hash SHA-256 convertido para string hexadecimal.
- Cada refresh token armazenado possui `createdAt`, `expiresAt`, `isRevoked` e `userId`.
- Refresh tokens novos expiram em 7 dias a partir da criacao.
- `RefreshToken.TokenHash` possui indice unico.

## Cookie Do Refresh Token

- O cookie do refresh token MUST se chamar `tracklink_refresh_token`.
- `HttpOnly` MUST ser `true`.
- `SameSite` MUST ser `Lax`.
- `Path` MUST ser `/api/auth`.
- `Domain` MUST NOT ser explicitamente definido.
- A expiracao do cookie MUST acompanhar a expiracao do refresh token.
- `Secure` MUST ser `true` em producao.
- Em ambiente local de desenvolvimento via HTTP, `Secure` MAY ser `false`.
- A configuracao de `SameSite` MUST ser revista se frontend e API forem futuramente hospedados em um cenario realmente cross-site que exija envio do refresh cookie.

## Regras De Autenticacao E Autorizacao

Endpoints publicos:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/tracking/{token}`
- `GET /api/tracking/{token}/history`

Endpoints protegidos por Bearer JWT:

- `GET /api/auth/me`
- `POST /api/tracking`
- `GET /api/tracking/my`
- `PUT /api/tracking/{token}`
- `DELETE /api/tracking/{token}`
- `POST /mcp`

Operacoes de tracking que modificam dados MUST validar ownership pelo usuario autenticado.
