# TrackLink Specs

Estas especificacoes descrevem o comportamento esperado do TrackLink conforme implementado atualmente no codigo-fonte, nos testes de integracao, no README e na configuracao do projeto.

As specs MUST servir como referencia para implementacao e testes. Quando houver divergencia entre documentacao existente e comportamento comprovado por codigo/testes/configuracao, a implementacao atual MUST prevalecer e a divergencia SHOULD ser registrada.

## Fluxo De Trabalho

Mudancas futuras MUST seguir este fluxo:

```text
spec -> implementacao -> testes -> validacao contra a spec
```

Novas funcionalidades ou mudancas de comportamento MUST atualizar ou criar a spec correspondente antes da implementacao.

## Escopo Atual

- [authentication.md](authentication.md): registro, login, JWT, claims, refresh tokens, logout e autorizacao.
- [tracking.md](tracking.md): criacao, consulta publica, ownership, expiracao, atualizacao, encerramento e historico.
- [realtime.md](realtime.md): SignalR, hub de tracking, grupos e eventos emitidos.
- [mcp.md](mcp.md): endpoint MCP, autenticacao, transporte e tools existentes.
- [deployment.md](deployment.md): configuracao atual de .NET, PostgreSQL, EF Core migrations, Docker e variaveis.

Estas specs documentam somente o comportamento existente. Elas MUST NOT ser interpretadas como roadmap.
