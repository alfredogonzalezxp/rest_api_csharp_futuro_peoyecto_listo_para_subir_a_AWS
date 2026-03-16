using api.Dtos;
using api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/auth")]
    [ApiController]

    /*
    Declaration of class and inherits controllerBase
    Why ControllerBase?
    By inheriting from ControllerBase, your 
    AuthController
    instantly gains access to helper methods for API responses, such as:
    Ok() (returns HTTP 200)
    BadRequest() (returns HTTP 400)
    NotFound() (returns HTTP 404)
    User (access to the current logged-in user)
    */
    public class AuthController : ControllerBase
    {
        /*
        This line defines a private field to store the Authentication Service.
        
        WHERE DOES IT COME FROM?
        - IAuthService is an INTERFACE defined in "Services/AuthService.cs".
        - It is NOT the class itself, but the "Contract" (the list of methods: Login, Register).
        
        HOW DOES IT GET HERE?
        - In "Program.cs", we wrote: builder.Services.AddScoped<IAuthService, AuthService>();
        - This tells the system: "Whenever a Controller asks for 'IAuthService', give them a brand new 'AuthService'."
        */
        private readonly IAuthService _authService;
        /*
        his is tue constructor of the principal class
            public AuthController(IAuthService authService)
            {
                _authService = authService;
            }
    I give the authservice value to the _authService and this is 
    used for call the methods of his class isnt?
    Yes, exactly! You have perfectly understood the concept.
        */
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /*
         * Q: WHERE IS THIS CLASS INITIALIZED (new AuthController...)?
         * A: It is done AUTOMATICALLY by the framework (ASP.NET Core).
         * 
         * 1. You NEVER write "new AuthController()" yourself.
         * 
         * 2. When a request comes in (POST /api/auth/login), the framework:
         *    a. Looks at "Program.cs" -> builder.Services.AddControllers();
         *    b. Sees that AuthController needs an IAuthService.
         *    c. Creates the AuthService first.
         *    d. Creates the AuthController and passes the AuthService to it.
         *    
         * This "Magic" is called Dependency Injection (DI).
         */

        /*
        This line defines the "Signup" endpoint.
        
        [HttpPost("signup")]:
        - Tells the server: "This function responds to POST requests sent to /api/auth/signup".
        
        public:
        - Accessible from outside (the web server needs to see it).
        
        async Task<IActionResult>:
        - async: Runs in the background so the server doesn't freeze while waiting.
        - Task<...>: Represents work that will finish in the future.
        - IActionResult: The result can be anything HTTP-related (Success, Error, Not Found, etc.).
        
        Signup:
        - The name of the function.
        
        [FromBody] RegisterDto registerDto:
        - [FromBody]: "Look inside the JSON attached to the request, not the URL."
        - RegisterDto: "Expect a JSON object that matches this shape (Name, Email, Password)."
        - registerDto: "Put that data into this variable so I can use it."
        - so with formbody i tell take from frombody the data to login in this case registerdto isnt?
        Yes, exactly! You have perfectly understood the concept.
        */
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] RegisterDto registerDto)
        /*
         * BREAKDOWN of: Signup([FromBody] RegisterDto registerDto)
         * 
         * 1. Signup: 
         *    - This is the name of the method. It's like the name of the "Job" 
         *      this specific receptionist does.
         * 
         * 2. [FromBody]: 
         *    - The "Source Instruction". 
         *    - It tells the API: "The data for this job isn't in the URL, it's 
         *      hidden inside the Body of the request (the payload)."
         * 
         * 3. RegisterDto: 
         *    - The "Blueprint" (Data Type).
         *    - It tells the API what SHAPE the data should have (Nombre, Email, Password).
         *    - This refers to the record you created in AuthDtos.cs.
         * 
         * 4. registerDto: 
         *    - The "Variable Name".
         *    - Once the API opens the envelope and finds the data, it puts it 
         *      into this variable so you can use it in the code (e.g., passing it to the service).
         */
        /*
         * EXPLANATION: How [FromBody] works (Data Mapping)
         * 
         * 1. The Request Body:
         *    - When the frontend sends a request, it includes a JSON object like:
         *      { "nombre": "Juan", "email": "juan@test.com", "password": "123", "rol": "User" }
         * 
         * 2. [FromBody]:
         *    - This attribute tells the API: "Open the request's envelope and look at the JSON inside."
         * 
         * 3. Model Binding (The Mapping):
         *    - ASP.NET automatically performs "Modeling Binding". It matches the keys in the JSON 
         *      to the names in your RegisterDto record.
         *    - "nombre" from JSON -> registerDto.Nombre
         *    - "email" from JSON -> registerDto.Email
         *    - ... and so on.
         * 
         * 4. Validation:
         *    - It also checks the [Required] and [EmailAddress] tags you put in AuthDtos.cs.
         *    - If the email is missing or invalid, the API stops here and returns an error.
         */
        {
            /*
            his code 
            var user = await _authService.Register(registerDto);
            return Ok(new { message = "User registered successfully", 
            userId = user.Id });

            says that the service add a user with service method register 
            using the authDtos public record isnt?
            Yes, exactly! You have perfectly understood the concept.
            */
            var user = await _authService.Register(registerDto);
            return Ok(new { message = "User registered successfully", userId = user.Id });
            /*
            so _context.Users.FindAsync(id); is taken from
             the class AppDbContext  who inherits DbContext.

            
            */
        }
        /*
        below is the same explanation like above.
        */
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var token = await _authService.Login(loginDto);
            return Ok(new { token });
        }
    }
}
