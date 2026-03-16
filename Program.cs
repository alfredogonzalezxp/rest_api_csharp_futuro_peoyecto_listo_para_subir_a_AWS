//.Here is the main file of the application
// And have the gunctions like securityconfig in java
using Amazon.Lambda.AspNetCoreServer.Hosting;
using api.Common;
using api.Data;
using api.Services;
using api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

/*
 * SUMMARY EXPLANATION:
 * var builder = WebApplication.CreateBuilder(args);
 * 
 * 1. The "Builder":    
 *    - Think of this as the "Construction Site Manager".
 *    - It prepares everything the app needs BEFORE the app actually starts running.
 *    - It loads configuration (appsettings.json).
 *    - It sets up the "Dependency Injection Container" (builder.Services).
 * 
 * 2. Where is "args"?
 *    - In modern C# (Top-Level Statements), the "Main" method is hidden.
 *    - "args" is a special variable that magically exists.
 *    - It holds any Command Line Arguments passed when starting the app 
 *      (e.g., dotnet run --seed-data).
 */
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Database

/*
 * SUMMARY EXPLANATION:
 * 
 * 1. Get the Connection String:
 *    - Looks inside "appsettings.json" for the "DefaultConnection" string.
 *    - This string contains the URL, Username, and Password for your PostgreSQL database.
 */
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

/*
 * 2. Setup the Database Context:
 *    - AddDbContext: Tells the app "I have a class called AppDbContext, please manage it for me."
 *    - UseNpgsql: "We are using PostgreSQL as our database software."
 *    - Service Lifetime: By default, this is "Scoped" (created once per HTTP request).
 *    - this builder.Services.AddDbContext is from using libraries.
 *    - In AppDbContext are the instructions for the database.
 *    - This is only for one database - if you have more than one 
 *    - database, you need to add more than one AddDbContext.
 *    - In case that you need to add other feature like courses table 
*     - you need to add  public DbSet<courses> Courses { get; set; }
* so. here.
  builder.Services.AddDbContext<AppDbContext>(options =>
  AppDbContext is used here because inherits from DbContext 
  and this use some database functionality?
 */
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
/*

/*
Yes, you are 95% correct! The better word than "dynamic" is 
"Service with Dependencies".

Here is the Golden Rule:

1. Put it in builder.Services (AddScoped) IF:
It needs other tools to work (e.g.,
AuthService
 needs 
AppDbContext
).
It holds data for the current request (e.g., "CurrentUser").
You want to be able to swap it later (Interface 
IAuthService
 -> Class 
AuthService
).
You invoke it by asking the Constructor for it.

In the constructor of AuthService.cs needs other tools to work like
IPasswordHasher and IJwtProvider and AppDbContext.

*/
// 2. Transients / Scoped Services
/*
 * EXPLANATION: Why <Interface, Class>?
 * 
 * so if the class and his interfaces are part of 
   a injection of a constructor is added to scoped in 
 *  program.cs isnt?  Yes it is.

 * 
 * WHEN TO USE 'AddScoped':
 * 1. Database Connections: Anything that holds a DB session.
 * 2. Per-Request Data: Services that track the "Current User".
      - Services that manage users.
 * 3. Most Logic: Standard "Business Logic" services in a Web API are usually Scoped.
 * So i can make services like courseservice for example that needs to save data and when this 
 * Service make a operation like add or subsctractt  
 * and then write in the datasbase so fot that that 
 * needs to be scoped, and because when an user use this business logic
 * when a session ends ends all like the service and his logic.
 * 
 * ALTERNATIVES:
 * - AddTransient: For stateless tools (Calculators). Created every time.
 * - AddSingleton: For global state (Cache). Created ONCE forever.
 * 
 * QUESTION: "PasswordHasher has no dependencies. Why is it Scoped?"
 * ANSWER:
 * 1. To be Injectable: Even if IT doesn't need tools, AuthService needs IT.
 *    - We register it so AuthService can receive it in the constructor.
 * 
 * 2. Why Scoped? (Consistency):
 *    - (e.g. You cannot inject a Scoped item into a Singleton item).
 *    
 * FINAL SUMMARY (The Golden Rule):
 * "If you see it in a Constructor: public MyClass(ITool tool)...
 *  ...You MUST register 'ITool' in this file (builder.Services.Add...)."
 */
/*
 * EXPLANATION:
 * builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
 * 
 * 1. Purpose:
 *    - Registers the "PasswordHasher" tool so it can be used by other parts of the app (like AuthService).
 * 
 * 2. <IPasswordHasher, PasswordHasher>:
 *    - The Interface (IPasswordHasher) is the "Contract" (what it does: Hash, Verify).
 *    - The Class (PasswordHasher) is the "Implementation" (how it does it: using HMACSHA256).
 *    - When code asks for "IPasswordHasher", it gets a new "PasswordHasher".
 */
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
/*
 * EXPLANATION:
 * builder.Services.AddScoped<IJwtProvider, JwtProvider>();
 * 
 * 1. Purpose:
 *    - Registers the "JwtProvider" tool. This is the "ID Card Maker".
 *    - Its job is to generate the encrypted Token string when a user logs in successfully.
 * 
 * 2. <IJwtProvider, JwtProvider>:
 *    - Interface: Methods like "GenerateToken(User user)".
 *    - Class: The actual code that uses the Secret Key to sign the token.
 */
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
/*
 * EXPLANATION:
 * builder.Services.AddScoped<IAuthService, AuthService>();
 * 
 * 1. Purpose:
 *    - Registers the "Main Logic" for authentication.
 *    - This class connects everything together: it takes the User's input, 
 *      checks the Database (AppDbContext), verifies passwords (PasswordHasher), 
 *      and issues tokens (JwtProvider).
 * 
 * 2. <IAuthService, AuthService>:
 *    - Interface: The Menu (Login, Register).
 *    - Class: The Kitchen (The code that actually cooks the meal).
 */
builder.Services.AddScoped<IAuthService, AuthService>();

// 3. Authentication
/*
 * SUMMARY EXPLANATION:
 * builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
 * 
 * 1. AddAuthentication():
 *    - "Turn ON the Security System". 
 *    - This registers the Authentication services into the DI container.
 *    - Without this line, [Authorize] attributes on Controllers would do NOTHING.
 * 
 * 2. JwtBearerDefaults.AuthenticationScheme:
 *    - "Set the DEFAULT method to JWT Bearer".
 *    - This is just the string "Bearer". It tells the system:
 qw3*      "When checking if someone is logged in, look for a JWT token 
 *       in the 'Authorization' header of the request."
 *    - Example header: Authorization: Bearer eyJhbGci...
 * 
 * 3. What happens next (.AddJwtBearer):
 *    - The chained .AddJwtBearer() below configures HOW to validate 
 *      that token (check issuer, audience, expiry, and signature).
 * 
 * ANALOGY:
 *    - AddAuthentication = "Install the security door at the entrance"
 *    - JwtBearerDefaults  = "The door uses ID Cards (JWT tokens) to verify people"
 *    - AddJwtBearer        = "Here are the rules for checking if the ID Card is real"
 */
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        /*
         * SUMMARY EXPLANATION:
         * 
         * 1. Token Validation Parameters:
         *    - This tells the Security Guard HOW to check if an ID Card (Token) is fake.
         *    - ValidateIssuer: "Did WE sign this?" (Check the Issuer string).
         *    - ValidateAudience: "Is this for US?" (Check the Audience string).
         *    - ValidateLifetime: "Is it expired?" (Check the Expiry Date).
         *    - IssuerSigningKey: "Does the signature match our Secret Key?" (The most important check).
         -*/
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            /*
            Here jwt:issuer is taken from appsettings.json
            And jwt:Audience es taken form asssettings.json
            */
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            /*
             * EXPLANATION: IssuerSigningKey
             * 
             * 1. builder.Configuration["Jwt:Key"]!:
             *    - Retrieves your secret password string from appsettings.json.
             *    - The '!' tells the compiler "I promise this is not null/empty".
             *    
             * 2. Encoding.UTF8.GetBytes(...):
             *    - Computers do math with numbers, not text.
             *    - This converts your secret string into a byte array (numbers).
             *    
             * 3. new SymmetricSecurityKey(...):
             *    - Creates a "Key Object" from those bytes.
             *    - "Symmetric" means the SAME key is used to:
             *      a) SEAL the token (when user logs in).
             *      b) CHECK the token (when user makes a request).
             *      
             * 4. IssuerSigningKey = ...:
             *    - This sets the key that the system will use to validate incoming tokens.
             *    - If the token was signed with a different key, it will be rejected.
               */
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// 4. Authorization
/*
 * EXPLANATION: Authorization
 * 
 * 1. Purpose:
 *    - "Authentication" (above) is checking ID: "Who are you?" (e.g. Alfrredo).
 *    - "Authorization" (this line) is checking Permissions: "Are you allowed here?" (e.g. Admin only).
 *    
 * 2. What this line does:
 *    - It adds the "Rule Book" services to the app.
 *    - It allows you to use [Authorize] on your Controllers.
 *    - It allows you to create specific Policies (e.g. "MustBeOver18").
 */
builder.Services.AddAuthorization();

// 5. CORS
builder.Services.AddCors(options =>
{
    /*
     * EXPLANATION: CORS Policy ("Cross-Origin Resource Sharing")
     * 
     * 1. AddPolicy("AllowAll", ...):
     *    - We are creating a specific rule named "AllowAll".
     *    - Later in the code (app.UseCors("AllowAll")), we will tell the app to USE this rule.
     * 
     * 2. WithOrigins("http://localhost:3000"):
     *    - TRUSTED WEBSITES: This specifies WHICH websites are allowed to talk to this API.
     *    - Browsers block requests from different websites by default for security.
     *    - This line says: "If the request comes from localhost:3000 (React/Vue/etc), let it in."
     * 
     * 3. AllowAnyMethod():
     *    - ALLOWED ACTIONS: Allows GET, POST, PUT, DELETE, etc.
     *    - Without this, maybe only GET would be allowed.
     * 
     * 4. AllowAnyHeader():
     *    - ALLOWED INFO: Allows custom headers (like "Authorization: Bearer ...").
     *    - Without this, the browser might block the JWT token.
     */
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Example frontend URL
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

/*
 * EXPLANATION: Final Setup & Building
 * 
 * 1. AddControllers():
 *    - Registers the "Controller" system.
 *    - It tells the app: "Scan my project for classes that look like API Controllers (Public class UsersController : ControllerBase) and use them."
 *    - THIS IS THE LINE that makes "new AuthController()" happen automatically.
 * 
 * 2. AddEndpointsApiExplorer() & AddSwaggerGen():
 *    - These are for DOCUMENTATION (Swagger).
 *    - ApiExplorer: "Looks at" your controllers to understand what URLs (endpoints) exist.
 *    - SwaggerGen: Generates the "Instruction Manual" (OpenAPI spec) from what the Explorer found.
 */
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

/*
 * EXPLANATION: The "Build" Command
 * 
 * 1. What this does:
 *    - Before this line, we were just "Writing the Recipe" (configuring options, adding tools).
 *    - This line is "Baking the Cake".
 *    - It effectively "locks in" all the configurations and creates the actual 'app' object that validates requests.
 */
var app = builder.Build();
/*
 * SUMMARY EXPLANATION: Development Mode vs Production Mode
 * 
 * 1. What are "Environments"?
 *    - They are a way for your app to know WHERE it is running.
 *    - The app reads the variable: ASPNETCORE_ENVIRONMENT
 * 
 * 2. Development Mode (Your PC):
 *    - When you run "dotnet run" on your machine, it defaults to "Development".
 *    - This is set in "Properties/launchSettings.json".
 *    - Purpose: Show full error details, Swagger UI, and debugging tools.
 *    - You WANT this so you can test and fix bugs easily.
 * 
 * 3. Production Mode (Real Server - AWS, Azure, etc.):
 *    - When deployed to a live server, it is set to "Production".
 *    - Purpose: Hide error details (security), disable Swagger, optimize performance.
 *    - You DO NOT want users or hackers to see your internal API docs or stack traces.
 * 
 * 4. app.Environment.IsDevelopment():
 *    - This method checks: "Is ASPNETCORE_ENVIRONMENT set to 'Development'?"
 *    - If YES → enable Swagger (the interactive API testing page).
 *    - If NO  → skip Swagger (users on the real server don't need it).
 * 
 * ANALOGY:
 *    - Development = "Kitchen" (you see all the mess, ingredients, and recipes)
 *    - Production  = "Restaurant" (customers only see the final plate, not the kitchen)
 */
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


/*
 * EXPLANATION: HTTPS Redirection
 * 
 * 1. Purpose:
 *    - Enforces security by redirecting HTTP requests to HTTPS.
 * 
 * 2. Scenario:
 *    - If a user types "http://localhost:5000", the server responds:
 *      "Please go to https://localhost:5001 instead."
 *    
 * 3. Why:
 *    - Validates that data (passwords, tokens) is always encrypted in transit.
 */
app.UseHttpsRedirection();

/*
 * EXPLANATION: Registering the Custom Exception Middleware
 * 
 * 1. Purpose:
 *    - Tells the app to use our custom "Safety Net" class (ExceptionMiddleware).
 * 
 * 2. Why here?
 *    - Middleware order matters!
 *    - We place it very early (before Auth, before Controllers).
 *    - This ensures it can catch errors from ALMOST ANYWHERE in the application.
 */
app.UseMiddleware<api.Middleware.ExceptionMiddleware>(); // Global Error Handling

/*
 * EXPLANATION: Enable Cross-Origin Resource Sharing (CORS)
 * 
 * 1. Purpose:
 *    - Validates if the incoming request is from a trusted website.
 *    - Uses the rule "AllowAll" that we defined earlier in builder.Services.AddCors.
 *    
 * 2. Scenario:
 *    - Browser: "Hi, I'm from localhost:3000 (React). Can I get data?"
 *    - Server: "Let me check my rules... Yes, 'AllowAll' says you are okay."
 *    
 * 3. Without this:
 *    - The browser will BLOCK the response and show a red error in the console.
 */
app.UseCors("AllowAll"); // Enable CORS

/*
 * EXPLANATION: Security Checkpoints
 * 
 * 1. app.UseAuthentication():
 *    - "The Bouncer".
 *    - Checks the Badge (JWT Token). "Who are you?"
 *    - If valid, sets the "User" property on the context.
 * 
 * 2. app.UseAuthorization():
 *    - "The VIP List".
 *    - Check the Permissions. "Are you allowed in this room?"
 *    - Looks at [Authorize] attributes on Controllers.
 *    
 * IMPORTANT: Order matters! You must know WH someone is (AuthN) before checking WHAT they can do (AuthZ).

app.UseAuthentication(); // 1. Turn on the "ID Badge 
// Scanner" (Check WHO they are)
app.UseAuthorization();  // 2. Turn on the "Security 
// Guard" (Check WHAT they can do)
2. The Usage (
UsersController.cs
) This is where you actually apply the security. Look at 
line 12 in 
UsersController
:

csharp
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
[Authorize]: This attribute tells the app: "Stop! Before 
running any code in this class, go back to 
Program.cs
, run UseAuthentication to see if they are logged in, and 
run UseAuthorization to see if they have the 'Admin' role."

 */
app.UseAuthentication();
app.UseAuthorization();
//--------------------------------------------
/*
 * EXPLANATION: Connecting the Wires
 * 
 * 1. app.MapControllers():
 *    - Finds all the [Route] attributes in your Controller classes.
 *    - Creates a map: "/api/users" -> UsersController.GetUsers()
 *    - This is what actually makes the URLs work.
 */
app.MapControllers();

/*
 * EXPLANATION: Seeding the Database
 * 
 * 1. Purpose:
 *    - Populates the database with initial data (users, roles, etc.)
 * 
 * 2. Why here?
 *    - We place it in 
Program.cs
 because it needs to run ONCE when the app starts.
 *    - We don't want to run it every time a user makes a request.
 * 
 * 3. How it works:
 *    - We create a "scope" to get the services we need.
 *    - We get the 
AppDbContext
 and 
IPasswordHasher
 from the container.
 *    - We call 
DbInitializer.Initialize
 to create the initial data.
 */
// Seed the database
/*
 * SUMMARY EXPLANATION: Why "CreateScope"?
 * 
 * 1. The Problem:
 *    - Services like AppDbContext are "Scoped" (created once per web request).
 *    - At this point (Startup), there is NO web request happening yet.
 * 
 * 2. The Solution:
 *    - .CreateScope() creates a temporary "manual workspace".
 *    - It simulates a web request so we can safely ask for Scoped services (DB, PasswordHasher).
 * 
 * 3. The "using" block:
 *    - Ensures that as soon as the seeding is done, the temporary workspace is 
 *      destroyed and the database connection is closed (memory management).
 */
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        // Ensure database is created (without migrations)
        context.Database.EnsureCreated();

        /*
         * SUMMARY EXPLANATION: Why is DbInitializer NOT in builder.Services?
         * 
         * 1. It is STATIC:
         *    - DbInitializer is a simple static helper class.
         *    - It doesn't need to be managed by the "Toolbox" because it doesn't store state.
         * 
         * 2. The "Once-Only" Rule:
         *    - This runs every time the app starts, but it only ACTUALLY inserts data 
         *      the VERY FIRST TIME (when the database is completely empty).
         *    - It checks "if (context.Users.Any())" - if users exist, it skips seeding.
         *    - This prevents it from creating duplicate Admins or overwriting your data later.
         */
        DbInitializer.Initialize(context, passwordHasher);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.Run();
