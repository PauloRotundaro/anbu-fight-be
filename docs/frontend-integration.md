# Anbu Fight — especificação da API para o frontend

Documento de integração da API REST da academia de boxe e muay thai **Anbu Fight**.
Todos os contratos foram extraídos do documento OpenAPI da API em execução.

O frontend cobre a **área de gestão** (alunos, professores, planos, matrículas, cobranças, grade de
aulas, chamada e relatórios) e o **portal do aluno** (auto-cadastro, check-in, frequência, cobranças
e perfil).

> **Atualização:** todos os itens do documento "o que falta na API" foram implementados, exceto a
> foto do aluno, adiada por decisão de produto. As respostas às 9 perguntas em aberto estão na
> seção 14.

---

## 1. Convenções gerais

- **Base URL (desenvolvimento):** `http://localhost:5203` ou `https://localhost:7099`
- **Formato:** JSON em requisições e respostas; propriedades em `camelCase`
- **Documentação interativa:** `/swagger` (documento em `/openapi/v1.json`)
- **Enums** trafegam como texto (`"Monthly"`, não `0`)
- **Datas sem hora** (`birthdate`, `dueDate`, `date`) são strings `"YYYY-MM-DD"`
- **Horários de aula** (`startTime`, `endTime`) são strings `"HH:mm"`
- **Instantes** (`createdAt`, `checkedInAt`, `startsAt`, `paydAt`) são ISO 8601 **em UTC**:
  `"2026-08-02T22:00:00+00:00"`. Converta para o fuso local na exibição — uma aula das 19h em
  São Paulo chega como `22:00Z`
- **Ids** são GUIDs (string); **valores monetários** são decimais em BRL
- Campos opcionais nulos são **omitidos** da resposta, não vêm como `null`
- O **fuso da academia** é `America/Sao_Paulo` (configurável no backend). É ele que define "hoje",
  o que está vencido e qual é a aula do dia

---

## 2. Autenticação

Toda a API exige JWT, exceto `POST /api/auth/login`, `/register`, `/forgot-password` e
`/reset-password`.

```
Authorization: Bearer <accessToken>
```

### Fluxo

1. `POST /api/auth/login` → `accessToken` (15 min) e `refreshToken` (14 dias).
2. **401** com `WWW-Authenticate: Bearer error="invalid_token"` = access token expirou:
   chame `/api/auth/refresh` e refaça a requisição.
3. O refresh é **rotativo** — o token apresentado é sempre revogado. Substitua o par armazenado.
4. Se o refresh falhar, a sessão acabou: limpe o estado e volte para o login.

Implemente num interceptor único, com fila para não disparar vários refreshes em paralelo.

### Endpoints

| Método | Rota | Corpo | Resposta |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | `LoginCommand` | `200 AuthenticationResult` · `401` |
| POST | `/api/auth/register` | `RegisterCommand` | `201 CreatedResponse` · `400` · `409` |
| POST | `/api/auth/refresh` | `RefreshSessionCommand` | `200 AuthenticationResult` · `401` |
| POST | `/api/auth/logout` | `LogoutCommand` | `204` |
| GET | `/api/auth/me` | — | `200 AuthenticatedUserDto` |
| PATCH | `/api/auth/password` | `ChangePasswordCommand` | `204` · `400` · `401` |
| POST | `/api/auth/forgot-password` | `{ email }` | `204` sempre |
| POST | `/api/auth/reset-password` | `{ token, newPassword }` | `204` · `400` |

`logout` com `{ "refreshToken": "..." }` encerra a sessão; com `{}` encerra **todas**.

```ts
interface LoginCommand { email: string; password: string; }
interface RefreshSessionCommand { refreshToken: string; }
interface LogoutCommand { refreshToken?: string | null; }

interface RegisterCommand {          // auto-cadastro público
  firstName: string;
  lastName: string;
  birthdate: string;                 // "YYYY-MM-DD", no passado
  email: string;                     // 409 se já existir
  phoneNumber: string;
  password: string;                  // obrigatório, 8–128
}

interface ChangePasswordCommand {
  currentPassword: string;
  newPassword: string;               // precisa ser diferente da atual
  currentRefreshToken?: string | null; // informe para manter esta sessão viva
}

interface AuthenticationResult {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthenticatedUserDto;
}

interface AuthenticatedUserDto {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;                 // false para aluno pendente ou inativo
  studentStatus: StudentStatus | null;
  studentId: string | null;
  teacherId: string | null;
}
```

### Cadastro pendente

`POST /api/auth/register` cria o aluno com `status: "PendingApproval"` — **sempre** com perfil
`Student`, ignorando qualquer `role` ou `status` enviado no corpo.

Quem está pendente **autentica normalmente**. O login devolve `isActive: false` e
`studentStatus: "PendingApproval"` — use isso para mostrar a tela de "cadastro em análise" em vez
do portal. O check-in continua bloqueado até a aprovação.

### Senhas sem e-mail configurado

`forgot-password` responde **204 sempre**, exista ou não o e-mail. Hoje o backend registra o link
de redefinição no log em vez de enviá-lo (não há provedor de e-mail configurado) — o fluxo funciona
de ponta a ponta, mas em produção depende de plugar um provedor. Enquanto isso, a rede de segurança
é a gestão redefinir a senha por `POST /api/students/{id}/portal-access`.

Trocar ou redefinir a senha **revoga as demais sessões**.

---

## 3. Perfis e permissões

```ts
type UserRole = "Admin" | "Teacher" | "Student";
type StudentStatus = "PendingApproval" | "Active" | "Inactive";
```

| Ação | Admin | Teacher | Student |
| --- | :---: | :---: | :---: |
| Listar alunos, aniversariantes, relatórios | ✅ | ✅ | ❌ |
| Ver um aluno / seu resumo | ✅ | ✅ | só a si mesmo |
| Criar, editar, excluir, aprovar, recusar aluno | ✅ | ❌ | ❌ |
| Professores, planos, aulas e sessões (leitura) | ✅ | ✅ | ✅ |
| Professores, planos, aulas (escrita) | ✅ | ❌ | ❌ |
| Matrículas e cobranças (leitura) | ✅ | ✅ | só as próprias |
| Matrículas e cobranças (escrita) | ✅ | ❌ | ❌ |
| Presenças (leitura) | ✅ | ✅ | só as próprias |
| Chamada manual e exclusão de presença | ✅ | ❌ | ❌ |
| Check-in | ❌ | ❌ | ✅ (só para si) |
| Editar o próprio perfil (`/students/me`) | — | — | ✅ |

Como a área do professor será usada com contas `Admin`, o perfil `Teacher` fica como leitura.

**Listagens de aluno:** sem filtro, um aluno já recebe só os próprios dados. Pedir
`?studentId=<outro>` devolve **403**. A mesma tela serve aos três perfis.

---

## 4. Listagens e paginação

`page` (padrão 1) e `pageSize` (padrão 20, **máximo 100**; acima disso → 400).

```ts
interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
```

As rotas de **sessões de aula** e **aniversariantes** devolvem array simples, sem envelope.

---

## 5. Erros

RFC 7807 (`application/problem+json`).

| Status | Quando | Como tratar |
| --- | --- | --- |
| `400` | Validação | Destacar os campos (ver abaixo) |
| `401` | Sem token, token inválido/expirado, credenciais erradas | Renovar ou ir ao login |
| `403` | Perfil sem permissão, ou dado de outra pessoa | Mensagem de acesso negado |
| `404` | Não existe (ou foi excluído) | Tela de "não encontrado" |
| `409` | Regra de negócio | **Mostrar `detail` ao usuário** — é texto pronto |
| `500` | Inesperado | Mensagem genérica |

```jsonc
// 400
{
  "type": "...", "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "FirstName": ["'First Name' deve ser informado."] },
  "traceId": "00-...-00"
}
// 409
{ "type": "...", "title": "...", "status": 409, "detail": "Você já registrou presença nesta aula.", "traceId": "..." }
```

⚠️ **Chaves de `errors` vêm em PascalCase** (`FirstName`) e o corpo enviado é camelCase
(`firstName`). Converta a inicial ao associar ao campo.

⚠️ **Mensagens misturadas** entre português e inglês: as regras específicas do domínio estão em
português, mas as regras genéricas do validador saem no idioma da máquina. Prefira mapear por chave
de campo e escrever o texto no frontend. Os `detail` de 409 estão todos em português e podem ser
exibidos direto.

⚠️ **403 tem dois formatos:** por perfil (corpo **vazio**) e por dono do dado (ProblemDetails).
Trate a ausência de corpo.

⚠️ Rotas inexistentes respondem **401**, não 404 — consequência da política que exige autenticação
por padrão.

---

## 6. Contratos

```ts
type PlanType = "Monthly" | "Quarterly" | "Semiannual" | "Annual";
type PaymentStatus = "Pending" | "Paid" | "Overdue";
type UserRole = "Admin" | "Teacher" | "Student";
type StudentStatus = "PendingApproval" | "Active" | "Inactive";
type Modality = "Boxing" | "MuayThai";
type AttendanceOrigin = "Student" | "Teacher";
type CheckInBlockReason = "Ok" | "InactiveStudent" | "NoActiveEnrollment" | "OverdueLimitExceeded";

interface StudentDto {
  id: string;
  firstName: string; lastName: string; fullName: string;
  birthdate: string;
  email: string; phoneNumber: string;
  status: StudentStatus;
  isActive: boolean;                 // derivado: status === "Active"
  emergencyContact: string | null;
  emergencyPhoneNumber: string | null;
  role: UserRole;
  hasPortalAccess: boolean;
  createdAt: string; updatedAt: string;
}

interface TeacherDto {
  id: string;
  firstName: string; lastName: string; fullName: string;
  birthdate: string; email: string; phoneNumber: string;
  role: UserRole; hasPortalAccess: boolean;
  createdAt: string; updatedAt: string;
}

interface PlanDto {
  id: string; name: string;
  type: PlanType;
  monthsInCycle: number;             // 1 | 3 | 6 | 12
  defaultValue: number | null;       // valor de tabela, para pré-preencher a matrícula
  enrollmentCount: number;
  createdAt: string; updatedAt: string;
}

interface StudentPlanDto {           // matrícula
  id: string;
  studentId: string; studentName: string;
  planId: string; planName: string; planType: PlanType;
  planValue: number;                 // valor negociado com este aluno
  dueDate: string;                   // vencimento da PRÓXIMA cobrança
  createdAt: string; updatedAt: string;
}

interface PaymentDto {               // cobrança
  id: string;
  studentId: string; studentName: string;
  studentPlanId: string; planId: string; planName: string;
  planValue: number;                 // preço congelado na emissão
  value: number;
  dueDate: string;
  paydAt: string | null;             // null = em aberto (grafia da API)
  status: PaymentStatus;             // derivado
  createdAt: string; updatedAt: string;
}

interface ClassDto {                 // aula recorrente da grade
  id: string; name: string;
  modality: Modality;
  daysOfWeek: number[];              // 0 = domingo … 6 = sábado
  startTime: string;                 // "19:00"
  endTime: string;                   // "20:00"
  teacherId: string; teacherName: string;
  capacity: number | null;           // null = sem limite
  isActive: boolean;
  createdAt: string; updatedAt: string;
}

interface ClassSessionDto {          // ocorrência concreta
  id: string;
  classId: string; className: string;
  modality: Modality;
  teacherId: string; teacherName: string;
  date: string;                      // "YYYY-MM-DD" (data local da academia)
  startsAt: string; endsAt: string;  // instantes UTC
  capacity: number | null;
  attendanceCount: number;
  isCancelled: boolean;
  cancellationReason: string | null;
  alreadyCheckedIn: boolean;         // do aluno que chamou
  canCheckIn: boolean;               // false para quem não é aluno
  checkInUnavailableReason: string | null;  // texto pronto para exibir
}

interface AttendanceDto {
  id: string;
  studentId: string; studentName: string;
  classSessionId: string; classId: string; className: string;
  modality: Modality;
  sessionDate: string;
  checkedInAt: string;
  registeredBy: AttendanceOrigin;
}

interface CheckInEligibilityDto {
  studentId: string;
  canCheckIn: boolean;
  reason: CheckInBlockReason;
  hasOverdueDebt: boolean;
  daysOverdue: number;               // atraso da cobrança vencida mais antiga
  overdueAmount: number;
  overdueCount: number;
  graceDays: number;                 // tolerância configurada (hoje 5)
  graceDaysRemaining: number;
}

interface CreatedResponse { id: string; }
```

---

## 7. Alunos — `/api/students`

| Método | Rota | Perfil |
| --- | --- | --- |
| GET | `/api/students?search=&status=&page=&pageSize=` | Admin, Teacher |
| GET | `/api/students/birthdays?daysAhead=&onlyActive=` | Admin, Teacher |
| GET | `/api/students/{id}` | Admin, Teacher, o próprio |
| GET | `/api/students/{id}/summary?recentItems=` | Admin, Teacher, o próprio |
| PUT | `/api/students/me` | Student |
| POST | `/api/students` | Admin |
| PUT | `/api/students/{id}` | Admin |
| PATCH | `/api/students/{id}/approve` | Admin — 409 se não estiver pendente |
| PATCH | `/api/students/{id}/reject` | Admin — 409 se não estiver pendente |
| POST | `/api/students/{id}/portal-access` | Admin |
| DELETE | `/api/students/{id}` | Admin |

- `status=PendingApproval` é a **fila de aprovação**; `status=Inactive` são os desligados
- `search` casa nome, sobrenome e e-mail (parcial, ignora maiúsculas)
- `reject` faz exclusão lógica e **libera o e-mail** para novo cadastro

```ts
interface CreateStudentCommand {
  firstName: string; lastName: string;
  birthdate: string; email: string; phoneNumber: string;
  status?: StudentStatus;            // padrão "Active" (cadastro pela gestão)
  emergencyContact?: string | null;
  emergencyPhoneNumber?: string | null;
  role?: UserRole;                   // padrão "Student"
  password?: string | null;          // se informado (8–128), cria o login
}

interface UpdateStudentRequest {     // PUT, todos obrigatórios, sem senha
  firstName: string; lastName: string;
  birthdate: string; email: string; phoneNumber: string;
  status: StudentStatus;
  emergencyContact: string | null;
  emergencyPhoneNumber: string | null;
  role: UserRole;
}

interface UpdateMyProfileRequest {   // PUT /api/students/me
  firstName: string; lastName: string;
  phoneNumber: string;
  emergencyContact: string | null;
  emergencyPhoneNumber: string | null;
}                                    // e-mail, nascimento, status e perfil ficam de fora

interface SetPortalAccessRequest { password: string; }

interface BirthdayStudentDto {
  id: string; fullName: string;
  birthdate: string;
  age: number;                       // idade que vai completar
  daysUntilBirthday: number;         // 0 = hoje
  phoneNumber: string;
  status: StudentStatus; isActive: boolean;
}

interface StudentSummaryDto {        // GET /api/students/{id}/summary
  student: StudentDto;
  enrollments: StudentPlanDto[];
  recentPayments: PaymentDto[];      // recentItems (padrão 5)
  recentAttendances: AttendanceDto[];
  eligibility: CheckInEligibilityDto;
}
```

> **Aniversariantes** usam `daysAhead` (0–365, padrão 30) em vez de um intervalo `MM-DD`, o que
> elimina o caso especial da virada de ano. Resultado ordenado por `daysUntilBirthday`. Quem nasceu
> em 29/02 aparece em 01/03 nos anos não bissextos.

---

## 8. Professores, planos, matrículas e cobranças

### Professores — `/api/teachers`
`GET /` (`search`), `GET /{id}` para qualquer autenticado; `POST`, `PUT`, `DELETE` e
`POST /{id}/portal-access` para Admin. Contratos iguais aos de aluno, sem status e sem contato de
emergência.

### Planos — `/api/plans`
`GET /` (`search`, `type`), `GET /{id}` para qualquer autenticado; escrita para Admin.
`DELETE` → **409** se houver matrículas.

```ts
interface CreatePlanCommand { name: string; type: PlanType; defaultValue?: number | null; }
interface UpdatePlanRequest { name: string; type: PlanType; defaultValue: number | null; }
```

### Matrículas — `/api/student-plans`
`GET /` (`studentId`, `planId`), `GET /{id}` com a restrição do aluno; escrita para Admin.
`POST` → 409 se o aluno já tiver o mesmo plano. `DELETE` → 409 se houver cobrança em aberto.

```ts
interface CreateStudentPlanCommand { studentId: string; planId: string; planValue: number; dueDate: string; }
interface UpdateStudentPlanRequest { planValue: number; dueDate: string; }
```

### Cobranças — `/api/payments`

| Método | Rota | Perfil |
| --- | --- | --- |
| GET | `/api/payments?studentId=&studentPlanId=&status=&dueFrom=&dueTo=&page=&pageSize=` | todos (aluno restrito) |
| GET | `/api/payments/{id}` | todos (aluno restrito) |
| POST | `/api/payments` | Admin |
| POST | `/api/payments/bulk-generate` | Admin |
| PUT | `/api/payments/{id}` | Admin — 409 se quitada |
| PATCH | `/api/payments/{id}/settle` | Admin — 409 se quitada |
| DELETE | `/api/payments/{id}` | Admin |

```ts
interface CreatePaymentCommand {
  studentPlanId: string;
  value?: number | null;             // padrão: valor da matrícula
  dueDate?: string | null;           // padrão: próximo vencimento da matrícula
  paydAt?: string | null;            // informe para registrar já quitada
}
interface UpdatePaymentRequest { value: number; dueDate: string; }
interface SettlePaymentRequest { paydAt?: string | null; }   // corpo opcional; sem corpo = agora

interface BulkGeneratePaymentsCommand { month: string; planId?: string; studentId?: string; }  // "YYYY-MM"
interface BulkGenerationResult { month: string; created: number; skipped: number; }
```

`bulk-generate` é **idempotente**: rodar de novo não duplica, só aumenta `skipped`.

---

## 9. Aulas — `/api/classes`

| Método | Rota | Perfil |
| --- | --- | --- |
| GET | `/api/classes?modality=&dayOfWeek=&teacherId=&isActive=&page=&pageSize=` | autenticado |
| GET | `/api/classes/{id}` | autenticado |
| POST · PUT · DELETE | `/api/classes[/{id}]` | Admin |

```ts
interface CreateClassCommand {
  name: string;
  modality: Modality;
  daysOfWeek: number[];              // 1..n dias, 0–6, sem repetição
  startTime: string;                 // "19:00"
  endTime: string;                   // deve ser depois do início
  teacherId: string;                 // 404 se não existir
  capacity?: number | null;
  isActive?: boolean;                // padrão true
}
// UpdateClassRequest: mesmos campos, todos obrigatórios, sem id
```

- Uma aula que ocorre em vários dias é **um registro só**, com `daysOfWeek: [1, 3]`
- **409** se o professor já tiver aula em horário que se sobreponha num dia em comum
- **409** no `DELETE` se já houver presenças (desative em vez de excluir)
- Alterar horário ou dias **regenera as sessões futuras sem presença**; o passado fica intacto

---

## 10. Sessões de aula — `/api/class-sessions`

| Método | Rota | Perfil |
| --- | --- | --- |
| GET | `/api/class-sessions/today` | autenticado |
| GET | `/api/class-sessions?from=&to=&modality=&teacherId=` | autenticado |
| GET | `/api/class-sessions/{id}` | autenticado |
| PATCH | `/api/class-sessions/{id}/cancel` | Admin — `{ reason? }`, 409 se já cancelada |

Sem `from`/`to`, devolve os **próximos sete dias**. O período é limitado a **92 dias** (acima disso,
400). As ocorrências são materializadas na primeira consulta, então **o `id` é estável** entre
requisições e serve para check-in e cancelamento.

`/today` é o que alimenta o portal: cada sessão já vem com `alreadyCheckedIn`, `canCheckIn` e, se
bloqueado, `checkInUnavailableReason` pronto para exibir — sem precisar de uma segunda requisição.

---

## 11. Presenças e check-in — `/api/attendances`

| Método | Rota | Perfil |
| --- | --- | --- |
| POST | `/api/attendances/check-in` | Student — `{ classSessionId }` |
| GET | `/api/attendances/eligibility?studentId=` | aluno (o próprio) · Admin/Teacher com `studentId` |
| GET | `/api/attendances/summary?studentId=&from=&to=` | idem |
| GET | `/api/attendances?studentId=&classSessionId=&modality=&from=&to=&page=&pageSize=` | todos (aluno restrito) |
| POST | `/api/attendances` | Admin — `{ studentId, classSessionId, checkedInAt? }` |
| DELETE | `/api/attendances/{id}` | Admin |

O check-in devolve **409** com `detail` pronto para exibir quando:

- já há presença registrada nessa sessão
- a sessão está cancelada
- está fora da janela — de **60 minutos antes do início** até o fim da aula
- a turma está lotada (`attendanceCount >= capacity`)
- o aluno não está ativo, não tem matrícula, ou está com atraso acima da tolerância

**A regra de inadimplência é imposta no servidor**, não só sinalizada: mesmo chamando a API direto,
o check-in é recusado. O endpoint de elegibilidade existe para a tela mostrar o estado antes do
clique — e `graceDays` vem na resposta, então **não codifique "5" no frontend**.

```ts
interface AttendanceSummaryDto {
  studentId: string;
  from: string; to: string;          // padrão: últimos 90 dias
  totalCheckIns: number;
  byModality: { modality: Modality; count: number }[];
  currentStreakDays: number;         // dias consecutivos treinando; zera se o último treino
                                     // não foi hoje nem ontem
  lastCheckInAt: string | null;
  weeklyAverage: number;
}
```

A chamada manual (`POST /api/attendances`) **ignora janela de horário e regra de débito** — é
registro do que aconteceu, não liberação de acesso.

---

## 12. Relatórios — `/api/reports`

`GET /api/reports/financial-summary?from=&to=` — Admin e Teacher. Sem parâmetros, usa o mês atual.

```ts
interface FinancialSummaryDto {
  from: string; to: string;
  billedTotal: number;               // cobranças com VENCIMENTO no período
  receivedTotal: number;             // cobranças quitadas no período, pela data de PAGAMENTO
  pendingTotal: number;              // em aberto e dentro do prazo
  overdueTotal: number;              // em aberto e vencido
  overdueCount: number;
  activeStudents: number;
  newEnrollments: number;
  defaultRate: number;               // overdueTotal / billedTotal (0 se não houver faturamento)
  revenueByMonth: { month: string; billed: number; received: number }[];  // "YYYY-MM"
}
```

---

## 13. CORS

Configurado e ativo. Em desenvolvimento, sem lista definida, qualquer origem é aceita. Em outros
ambientes, apenas as origens de `Cors:AllowedOrigins` (com credenciais liberadas). Com o BFF isso
não é obrigatório, mas destrava Swagger e ferramentas externas.

---

## 14. Respostas às perguntas em aberto

| # | Pergunta | Resposta |
| --- | --- | --- |
| 1 | Login de aluno pendente | **Autentica normalmente.** `isActive: false` + `studentStatus` no login |
| 2 | `isActive` no `AuthenticatedUserDto` | **Sim**, mais `studentStatus` |
| 3 | `status` ou `isApproved` | **`status`** (`PendingApproval`/`Active`/`Inactive`) |
| 4 | Infraestrutura de e-mail | Fluxo pronto com remetente plugável; hoje **loga o link** em vez de enviar |
| 5 | Aula em vários dias | **`daysOfWeek: number[]`**, um registro só |
| 6 | Sessões materializadas ou calculadas | **Materializadas sob demanda** na leitura — id estável e real |
| 7 | Janela de check-in | **60 min antes até o fim**, configurável no backend |
| 8 | Tolerância de 5 dias | **Configurável** e exposta como `graceDays` na elegibilidade |
| 9 | Troca de senha invalida sessões | **Sim**; mande `currentRefreshToken` para preservar a atual |

---

## 15. Mudanças em relação à versão anterior desta spec

| O que mudou | Antes | Agora |
| --- | --- | --- |
| Situação do aluno | `isActive: boolean` | `status: StudentStatus` + `isActive` derivado na leitura |
| Criar/editar aluno | campo `isActive` | campo **`status`** |
| Filtro da listagem | `?isActive=true` | **`?status=Active`** |
| `AuthenticatedUserDto` | sem situação | ganhou `isActive` e `studentStatus` |
| `PlanDto` e escrita de plano | — | ganhou `defaultValue` |
| CORS | ausente | configurado |

Nada mais mudou: os contratos de professor, matrícula e cobrança seguem idênticos.

---

## 16. Regras de negócio que a interface precisa refletir

1. **Status da cobrança é derivado** de `paydAt` e `dueDate`. Use cores distintas para
   `Pending`/`Paid`/`Overdue`.
2. **Quitar avança a matrícula** um ciclo (mensal → +1 mês). Recarregue a matrícula depois.
3. **Cobrança quitada é imutável** — esconda editar/quitar quando `status === "Paid"`.
4. **Vários planos por aluno**, nunca o mesmo duas vezes: filtre o seletor de plano.
5. **Preço congelado na cobrança**: reajustar a matrícula não altera cobranças emitidas.
6. **Senha opcional** no cadastro pela gestão; `hasPortalAccess` diz se a pessoa acessa o portal.
7. **Alterar o e-mail altera o login** — avise no formulário.
8. **Excluir aluno/professor** derruba o acesso e preserva o histórico financeiro.
9. **Excluir plano** é bloqueado com matrículas; **excluir matrícula**, com cobrança em aberto;
   **excluir aula**, com presenças. Sempre 409 com o motivo em `detail`.
10. **Aluno pendente** vê a tela de análise, não o portal; **aluno inativo** mantém histórico.
11. **Aviso de débito** aparece sempre que `hasOverdueDebt` for true, mesmo com o check-in liberado —
    mostre `graceDaysRemaining`.
12. **Aula cancelada** continua na lista com `isCancelled` e o motivo: o aluno precisa saber que não
    vai ter treino.

---

## 17. Telas sugeridas

**Gestão** — dashboard (resumo financeiro + vencidos + aniversariantes) · fila de aprovação
(`?status=PendingApproval`) · alunos (busca, filtro por status, detalhe via `/summary`) ·
professores · planos · matrículas · cobranças (filtros de status e período, baixa, emissão em lote)
· grade de aulas · chamada do dia (sessões de hoje + lançamento manual) · relatório de frequência.

**Portal do aluno** — criar conta · aguardando aprovação · início (aula de hoje com botão de
check-in e aviso de débito) · minhas cobranças · minha frequência · meu perfil · trocar senha.

**Público** — login · esqueci minha senha · redefinir senha.
