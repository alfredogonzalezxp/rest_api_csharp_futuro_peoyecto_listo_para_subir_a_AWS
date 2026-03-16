# Application Flow — C# API

## Project Structure

```
api/
├── Program.cs                  → Entry point: DI, middleware, startup
├── appsettings.json            → Configuration (DB, JWT, Logging)
├── Common/
│   └── PasswordHasher.cs       → BCrypt password hashing (IPasswordHasher)
├── Controllers/
│   ├── AuthController.cs       → POST /api/auth/signup, POST /api/auth/login
│   ├── UsersController.cs      → CRUD /api/users (Admin only)
│   └── HealthController.cs     → GET /api/health
├── Data/
│   ├── AppDbContext.cs          → EF Core DbContext (Users table)
│   └── DbInitializer.cs        → Seeds default Admin user on first run
├── Domain/
│   └── User.cs                 → User entity (Id, Nombre, Email, Password, Rol)
├── Dtos/
│   └── AuthDtos.cs             → LoginDto, RegisterDto
├── Middleware/
│   └── ExceptionMiddleware.cs  → Global error handler (try-catch all requests)
└── Security/
    └── JwtProvider.cs           → JWT token generation (IJwtProvider)
```

---

## 1. Application Startup (`Program.cs`)

When you run `dotnet run`, the application boots up in this order:

### Step 1 — Create the Builder

```
var builder = WebApplication.CreateBuilder(args);
```

Loads `appsettings.json` and prepares the **Dependency Injection (DI) container**.

### Step 2 — Register Services (DI Container)

| Registration    | Interface → Class                    | Lifetime | Purpose                                                |
| --------------- | ------------------------------------ | -------- | ------------------------------------------------------ |
| Database        | —                                    | Scoped   | `AppDbContext` connected to PostgreSQL via `UseNpgsql` |
| Password Hasher | `IPasswordHasher` → `PasswordHasher` | Scoped   | BCrypt hash & verify                                   |
| JWT Provider    | `IJwtProvider` → `JwtProvider`       | Scoped   | Generate JWT tokens                                    |
| Auth Service    | `IAuthService` → `AuthService`       | Scoped   | Login & Register logic                                 |
| Authentication  | JWT Bearer                           | —        | Validates incoming JWT tokens                          |
| Authorization   | —                                    | —        | Enables `[Authorize]` attributes                       |
| CORS            | Policy `"AllowAll"`                  | —        | Allows `localhost:3000` frontend                       |
| Controllers     | `AddControllers()`                   | —        | Registers API controller classes                       |
| Swagger         | `AddSwaggerGen()`                    | —        | API documentation (dev only)                           |

### Step 3 — Build the App

```
var app = builder.Build();
```

Locks in all configurations and creates the runnable application.

### Step 4 — Configure Middleware Pipeline (Order Matters!)

```
1. Swagger UI              (Development only)
2. HTTPS Redirection       (Redirect HTTP → HTTPS)
 3. ExceptionMiddleware     (Global error catch — catches everything below)
4. CORS                    (Check origin policy)
5. Authentication          (Validate JWT token — "Who are you?")
6. Authorization           (Check permissions — "Are you allowed?")
7. MapControllers          (Route request to the correct Controller action)
```

### Step 5 — Seed the Database

Before `app.Run()`, the app:

1. Creates a DI scope
2. Gets `AppDbContext` and `IPasswordHasher`
3. Calls `context.Database.EnsureCreated()` (creates DB tables if missing)
4. Calls `DbInitializer.Initialize()` — inserts a default Admin user if the Users table is empty:
   - **Name:** Admin System
   - **Email:** admin@system.com
   - **Password:** Admin123! (hashed with BCrypt)
   - **Role:** Admin

### Step 6 — Run

```
app.Run();
```

Starts listening for HTTP requests.

---

## 2. Request Lifecycle (How Every Request Flows)

```
                         ┌──────────────────────┐
   HTTP Request ───────► │   Kestrel Web Server  │
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │  HTTPS Redirection    │
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │ ExceptionMiddleware   │  ◄── Wraps everything in try-catch
                         │  (Global Error Net)   │      If error → returns JSON error
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │   CORS Middleware     │  ◄── Checks if origin is allowed
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │   Authentication      │  ◄── Reads JWT from "Authorization"
                         │  (JWT Bearer Check)   │      header, validates signature,
                         └──────────┬───────────┘      sets HttpContext.User
                                    │
                         ┌──────────▼───────────┐
                         │   Authorization       │  ◄── Checks [Authorize] attributes
                         │  (Role / Policy)      │      e.g. Roles = "Admin"
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │   Router              │  ◄── Matches URL to Controller
                         │  (MapControllers)     │      e.g. /api/auth/login → AuthController.Login
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │   Controller Action   │  ◄── Runs the endpoint method
                         │  (e.g. Login())       │      Calls Service layer
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │   Service Layer       │  ◄── Business logic
                         │  (e.g. AuthService)   │      Talks to DB, hashes, generates JWT
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │   Database            │  ◄── PostgreSQL via EF Core
                         │  (AppDbContext)        │
                         └──────────┬───────────┘
                                    │
                         ◄──── Response flows back up through the pipeline ────►
```

---

## 3. API Endpoints & Their Flows

### 3.1 — `POST /api/auth/signup` (Register)

**No authentication required.**

```
Request Body: { "nombre": "Juan", "email": "juan@test.com", "password": "123", "rol": "User" }
```

**Flow:**

1. **AuthController.Signup()** receives `RegisterDto` (deserialized from JSON body)
2. Calls **AuthService.Register()**:
   - Checks if email already exists in DB → throws `Exception("Email already exists")` if so
   - Creates a new `User` object with hashed password (`PasswordHasher.Hash()`)
   - Saves to DB via `_context.Users.Add()` + `SaveChangesAsync()`
   - Returns the created `User`
3. Controller returns `200 OK` with `{ message, userId }`

### 3.2 — `POST /api/auth/login`

**No authentication required.**

```
Request Body: { "email": "admin@system.com", "password": "Admin123!" }
```

**Flow:**

1. **AuthController.Login()** receives `LoginDto`
2. Calls **AuthService.Login()**:
   - Finds user by email in DB (`FirstOrDefaultAsync`)
   - If not found or password doesn't match (`PasswordHasher.Verify()`) → throws `Exception("Invalid credentials")`
   - Calls **JwtProvider.Generate(user)**:
     - Reads secret key, issuer, audience from `appsettings.json`
     - Creates claims: `NameIdentifier` (user ID), `Email`, `Role`
     - Builds a `JwtSecurityToken` with 60-minute expiry
     - Signs it with HMAC-SHA256 and converts to string
   - Returns the JWT token string
3. Controller returns `200 OK` with `{ token: "eyJhbGci..." }`

### 3.3 — `GET /api/users` (List All Users)

**Requires: JWT Token + Admin role.**

```
Header: Authorization: Bearer <token>
```

**Flow:**

1. Authentication middleware validates the JWT token
2. Authorization middleware checks the token has `Role = "Admin"`
3. **UsersController.GetUsers()** queries all users from DB
4. Returns `200 OK` with `[{ id, nombre, email, rol }]` (password excluded)

### 3.4 — `GET /api/users/{id}` (Get User by ID)

**Requires: JWT Token + Admin role.**

**Flow:**

1. Auth checks pass (same as above)
2. **UsersController.GetUserById()** finds user by primary key (`FindAsync`)
3. Returns `200 OK` with `{ id, nombre, email, rol }` or `404 Not Found`

### 3.5 — `PUT /api/users/{id}` (Update User)

**Requires: JWT Token + Admin role.**

```
Request Body: { "nombre": "New Name", "email": "new@email.com", "rol": "User" }
```

**Flow:**

1. Auth checks pass
2. **UsersController.UpdateUser()** finds user, updates fields, saves to DB
3. Returns `200 OK` with `{ message: "User updated successfully" }` or `404 Not Found`

### 3.6 — `DELETE /api/users/{id}` (Delete User)

**Requires: JWT Token + Admin role.**

**Flow:**

1. Auth checks pass
2. **UsersController.DeleteUser()** finds user, removes from context, saves
3. Returns `200 OK` with `{ message: "User deleted successfully" }` or `404 Not Found`

### 3.7 — `GET /api/health` (Health Check)

**No authentication required.**

**Flow:**

1. **HealthController.Get()** returns `200 OK` with `"Ok..Running..."`

---

## 4. Error Handling Flow

When **any** exception is thrown anywhere in the pipeline:

```
Controller/Service throws Exception
        │
        ▼
ExceptionMiddleware catches it this is configurated in program.cs app.UseMiddleware<api.Middleware.ExceptionMiddleware>(); // Global Error Handling
        │
        ├── Logs the error (ILogger)
        ├── Sets response status to 500
        │
        ├── Development mode:
        │     Returns { "message": "...", "stackTrace": "..." }
        │
        └── Production mode:
              Returns { "message": "Internal Server Error" }
```

---

## 5. Dependency Injection Chain

When a request hits a controller, the DI container auto-creates the required objects:

```
AuthController needs:
  └── IAuthService (→ AuthService) which needs:
        ├── AppDbContext (→ PostgreSQL connection)
        ├── IPasswordHasher (→ PasswordHasher using BCrypt)
        └── IJwtProvider (→ JwtProvider) which needs:
              └── IConfiguration (→ appsettings.json)

UsersController needs:
  └── AppDbContext (→ PostgreSQL connection)
```

All services are **Scoped** — created once per HTTP request, then disposed.

---

## 6. Configuration (`appsettings.json`)

| Key                                   | Value                        | Used By                          |
| ------------------------------------- | ---------------------------- | -------------------------------- |
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | `AppDbContext`                   |
| `Jwt:Issuer`                          | `"api"`                      | `JwtProvider` + token validation |
| `Jwt:Audience`                        | `"api"`                      | `JwtProvider` + token validation |
| `Jwt:Key`                             | Secret signing key           | `JwtProvider` + token validation |
