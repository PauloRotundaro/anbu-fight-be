# Anbu Fight API

API de gestão da academia de boxe e muay thai **Anbu Fight**: alunos, professores, planos,
matrículas e cobranças.

.NET 10 · PostgreSQL 17 · Clean Architecture · CQRS com MediatR · JWT

---

## Como rodar

```bash
docker compose up -d
```

```bash
dotnet run --project src/AnbuFight.Api
```

O container publica a porta **5433** no host, deixando a 5432 livre para um PostgreSQL instalado
localmente.

As migrations são aplicadas automaticamente no startup (`DatabaseInitializerHostedService`), junto
com o seed do usuário administrador.

### Documentação interativa

Em desenvolvimento, três rotas expõem o mesmo documento OpenAPI:

| Rota | O que é |
| --- | --- |
| `/swagger` | Swagger UI |
| `/scalar/v1` | Scalar, uma alternativa ao Swagger UI |
| `/openapi/v1.json` | O documento OpenAPI 3.1 em si |

O documento é gerado pelo `Microsoft.AspNetCore.OpenApi` (nativo do .NET 10) — o Swagger UI apenas
o consome, então não há duas gerações concorrendo. Descrições de endpoints, contratos e enums vêm
dos comentários XML do código, via o gerador de origem do próprio pacote.

Para testar endpoints protegidos pelo Swagger UI: chame `POST /api/auth/login`, copie o
`accessToken` da resposta e informe-o em **Authorize**.

As rotas de documentação são registradas apenas em `Development`. Para expô-las em outro ambiente,
ajuste a condição em [Program.cs](src/AnbuFight.Api/Program.cs).

Credenciais iniciais (definidas em `appsettings.Development.json`):

| Campo | Valor |
| --- | --- |
| E-mail | `admin@anbufight.com` |
| Senha | `Anbu@Fight123` |

Em produção, `Jwt:SigningKey`, `ConnectionStrings:Default` e `Seed:AdminPassword` devem vir de
variáveis de ambiente ou user secrets — os valores versionados são apenas para desenvolvimento.

### Testes

```bash
dotnet test
```

Os testes de integração sobem um PostgreSQL descartável via Testcontainers, portanto exigem Docker
em execução.

---

## Arquitetura

```
src/
  AnbuFight.Domain           entidades e regras que não dependem de nada
  AnbuFight.Application      casos de uso (commands/queries), validações e contratos
  AnbuFight.Infrastructure   EF Core, PostgreSQL, JWT, hashing, seed
  AnbuFight.Api              Minimal APIs, autorização e tratamento de erros
tests/
  AnbuFight.Application.UnitTests   domínio, validators e criptografia
  AnbuFight.Api.IntegrationTests    HTTP ponta a ponta contra Postgres real
```

As dependências apontam sempre para dentro: `Api → Infrastructure → Application → Domain`. A camada
`Application` só conhece o banco através de `IApplicationDbContext`, o que mantém os casos de uso
livres do provider.

Cada caso de uso vive em um arquivo único contendo o *command/query*, seu *validator* e seu
*handler* — abrir um arquivo mostra a operação inteira.

---

## Modelo de dados

Todas as tabelas têm `Id` (GUID v7, sequencial), `CreatedAt`, `UpdatedAt` e `DeletedAt`.
O preenchimento é feito pelo `AuditableEntityInterceptor`, e o `DELETE` é sempre lógico: o
interceptor converte a remoção em `UPDATE deleted_at`, e um *query filter* global esconde a linha.

| Tabela | Descrição |
| --- | --- |
| `students` | Aluno, com contato de emergência e situação (`PendingApproval`, `Active`, `Inactive`) |
| `teachers` | Professor |
| `plans` | Plano, com recorrência de cobrança (`Monthly`, `Quarterly`, `Semiannual`, `Annual`) |
| `student_plans` | Matrícula N-M aluno ↔ plano, com valor negociado e próximo vencimento |
| `payments` | Cobrança emitida contra uma matrícula |
| `classes` | Aula recorrente da grade (modalidade, dias da semana, horário, professor, vagas) |
| `class_sessions` | Ocorrência concreta de uma aula num dia — o alvo do check-in |
| `attendances` | Presença de um aluno numa sessão |
| `users` | Credencial de acesso, opcionalmente ligada a um aluno ou professor |
| `refresh_tokens` | Sessões ativas (armazena apenas o hash do token) |
| `password_reset_tokens` | Tokens de redefinição de senha, de uso único (também só o hash) |

Decisões que valem destaque:

- **`Payment` referencia `StudentPlan`** (além de `StudentId`), então sempre se sabe qual matrícula
  a cobrança quita. `PlanValue` é copiado no momento da emissão para que reajustes futuros não
  reescrevam o histórico.
- **`Payment.Status`** (`Pending` / `Paid` / `Overdue`) é derivado de `PaydAt` e `DueDate` na própria
  consulta SQL — nunca é armazenado, então não tem como ficar desatualizado.
- **Quitar uma cobrança avança a matrícula** para o próximo ciclo, conforme o `Type` do plano.
- **Um aluno pode ter vários planos ativos**, mas não o mesmo plano duas vezes: índice único
  filtrado em `(student_id, plan_id) WHERE deleted_at IS NULL`. Como o filtro ignora registros
  removidos, uma matrícula cancelada pode ser recriada depois.
- **Sessões de aula são materializadas sob demanda**: ao consultar um período, as ocorrências que
  faltam são criadas e persistidas. A alternativa seria calculá-las a cada leitura, mas aí presença
  e cancelamento não teriam registro real para apontar. Assim o id é estável e há integridade
  referencial, sem depender de job agendado.
- **Check-in é regra de servidor**: janela de horário, lotação, situação do aluno e inadimplência
  são validadas no handler, não apenas sinalizadas para a interface.

---

## Autenticação e autorização

Login devolve um par de tokens: *access token* JWT de curta duração e *refresh token* opaco, salvo
apenas como hash SHA-256. O refresh é rotativo — o token apresentado é sempre revogado, então um
token roubado só serve uma vez.

| Rota | Descrição |
| --- | --- |
| `POST /api/auth/login` | Autentica e devolve o par de tokens |
| `POST /api/auth/register` | Auto-cadastro do aluno (nasce pendente de aprovação) |
| `POST /api/auth/refresh` | Troca o refresh token por um novo par |
| `POST /api/auth/logout` | Revoga o token informado, ou todas as sessões |
| `GET /api/auth/me` | Perfil do usuário autenticado |
| `PATCH /api/auth/password` | Troca a própria senha e encerra as demais sessões |
| `POST /api/auth/forgot-password` | Envia o link de redefinição (204 sempre) |
| `POST /api/auth/reset-password` | Consome o token e grava a nova senha |

Todos os demais endpoints exigem token — a política *fallback* da aplicação recusa qualquer
requisição não autenticada, de modo que esquecer um `RequireAuthorization` não abre uma brecha.

| Papel | Permissões |
| --- | --- |
| `Admin` | Acesso total |
| `Teacher` | Lê alunos, professores, planos, matrículas e cobranças |
| `Student` | Lê apenas os próprios dados |

A restrição do aluno é aplicada em duas camadas: a política de papel no endpoint e o `AccessGuard`
nos handlers, que estreita a consulta para o próprio aluno mesmo quando nenhum filtro é enviado.

Senhas usam PBKDF2-HMAC-SHA256 com 210.000 iterações e salt por usuário; o número de iterações é
gravado junto ao hash, então pode ser elevado depois sem invalidar as senhas existentes.

---

## Endpoints

Listagens são sempre paginadas (`page`, `pageSize`, máximo de 100 itens) e devolvem
`{ items, page, pageSize, totalCount, totalPages, hasNextPage, hasPreviousPage }`.

| Recurso | Rotas |
| --- | --- |
| Alunos | `GET /api/students` (`search`, `status`) · `GET /api/students/birthdays` · `GET\|PUT\|DELETE /api/students/{id}` · `GET /api/students/{id}/summary` · `POST /api/students` · `PUT /api/students/me` · `PATCH /api/students/{id}/approve\|reject` · `POST /api/students/{id}/portal-access` |
| Professores | `GET /api/teachers` (`search`) · `GET\|PUT\|DELETE /api/teachers/{id}` · `POST /api/teachers` · `POST /api/teachers/{id}/portal-access` |
| Planos | `GET /api/plans` (`search`, `type`) · `GET\|PUT\|DELETE /api/plans/{id}` · `POST /api/plans` |
| Matrículas | `GET /api/student-plans` (`studentId`, `planId`) · `GET\|PUT\|DELETE /api/student-plans/{id}` · `POST /api/student-plans` |
| Cobranças | `GET /api/payments` (`studentId`, `studentPlanId`, `status`, `dueFrom`, `dueTo`) · `GET\|PUT\|DELETE /api/payments/{id}` · `POST /api/payments` · `POST /api/payments/bulk-generate` · `PATCH /api/payments/{id}/settle` |
| Aulas | `GET /api/classes` (`modality`, `dayOfWeek`, `teacherId`, `isActive`) · `GET\|PUT\|DELETE /api/classes/{id}` · `POST /api/classes` |
| Sessões | `GET /api/class-sessions` (`from`, `to`, `modality`, `teacherId`) · `GET /api/class-sessions/today` · `GET /api/class-sessions/{id}` · `PATCH /api/class-sessions/{id}/cancel` |
| Presenças | `POST /api/attendances/check-in` · `GET /api/attendances` · `GET /api/attendances/eligibility` · `GET /api/attendances/summary` · `POST /api/attendances` · `DELETE /api/attendances/{id}` |
| Relatórios | `GET /api/reports/financial-summary` (`from`, `to`) |

`POST /api/students` e `POST /api/teachers` aceitam um campo opcional `password`: quando informado,
a credencial de acesso é criada na mesma transação.

Erros seguem RFC 7807 (`ProblemDetails`): `400` validação, `401` credenciais, `403` permissão,
`404` inexistente, `409` conflito de regra de negócio.

---

## Desempenho

- Consultas de leitura usam `AsNoTracking` e projetam direto para o DTO, então o Postgres devolve só
  as colunas necessárias e nenhuma entidade é materializada.
- `AddDbContextPool` reaproveita contextos e o cache de queries compiladas entre requisições.
- Índices desenhados para as consultas reais, incluindo índices parciais: cobranças em aberto
  (`WHERE payd_at IS NULL AND deleted_at IS NULL`) e as unicidades que ignoram registros removidos.
- Paginação obrigatória com teto de 100 itens por página.
- `Guid.CreateVersion7` mantém a inserção nos índices sequencial, sem a fragmentação de GUIDs
  aleatórios.
- Resiliência a falhas transitórias de conexão via `EnableRetryOnFailure`.

---

## Convenções e observações

- Datas de nascimento e vencimentos são `DateOnly` (`date` no Postgres); demais carimbos são
  `DateTimeOffset` (`timestamptz`), sempre em UTC. "Hoje" — o que está vencido, qual é a aula do dia
  e a janela de check-in — é resolvido no **fuso da academia** (`Gym:TimeZone`, padrão
  `America/Sao_Paulo`) pelo `IGymClock`, e não na data UTC.
- Políticas operacionais ficam em configuração, não em código: `Gym:OverdueGraceDays` (tolerância de
  atraso para o check-in) e `Gym:CheckInWindowMinutesBefore`. O frontend lê os valores da API em vez
  de duplicá-los.
- O envio de e-mail é uma interface (`IEmailSender`). Sem provedor configurado, a implementação
  padrão apenas registra a mensagem no log — o fluxo de "esqueci minha senha" funciona de ponta a
  ponta em desenvolvimento, bastando copiar o link do console.
- Enums trafegam como texto no JSON e são gravados como texto no banco.
- Alterar o e-mail de um aluno ou professor também altera o e-mail de login, já que o e-mail é a
  identidade de acesso.
- Excluir um aluno ou professor remove a credencial e revoga as sessões ativas; o histórico
  financeiro é preservado.
- Novas migrations: `dotnet dotnet-ef migrations add <Nome> --project src/AnbuFight.Infrastructure --output-dir Persistence/Migrations`.
