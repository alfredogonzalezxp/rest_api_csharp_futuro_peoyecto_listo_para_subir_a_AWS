using api.Common;
using api.Data;
using api.Domain;
using api.Dtos;
using api.Security;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{

    /*
    Explanation of "public interface IAuthService":

    1. public:
       - This is an "Access Modifier".
       - It allows other parts of the application (like Controllers) to see and use this interface.

    2. interface:
       - An Interface is a "Contract" or "Blueprint".
       - It lists the methods (Login, Register) that MUST be available.
       - It does NOT contain the actual code/logic (the "How"). It only defines the "What".
       
    Why do we use an Interface here? (The Big Picture)
       - Decoupling: The AuthController only knows about "IAuthService". It doesn't care if the actual logic is in "AuthService" or "SuperSecureAuthService".
       - Dependency Injection: This allows the system to automatically plug in the correct implementation (AuthService) when the app starts.
    */
    public interface IAuthService
    {
        /*
        Task<string>: This function promises to return a String.
        That string is the "Golden Ticket" (the JWT Token) that 
        the user needs to access the site.

        so. the infterface can be used fby everybody
        it doesnt matter who an the class cant.      

        */
        Task<string> Login(LoginDto loginDto);
        Task<User> Register(RegisterDto registerDto);
    }

    /*
        This class implements the IAuthService interface.
        It provides the actual implementation of the methods 
        defined in the interface.
    */
    public class AuthService : IAuthService
    {



        /*
        EXPLANATION OF THE FIELDS:
        These 3 lines define the "tools" or "dependencies" that this class needs to do its job.

        1. private readonly AppDbContext _context;
           - This is the Database Connection. We use it to save/load Users.
           - "private": Inside this file only.
           - "readonly": Can only be set once (in the constructor).

        2. private readonly IPasswordHasher _passwordHasher;
           - This is the Security Tool. We use it to hash passwords (make them unreadable) 
             and check if a password is correct.

        3. private readonly IJwtProvider _jwtProvider;
           - This is the Token Generator. We use it to create the "Access Key" (JWT) 
             when a user logs in successfully.
        */
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtProvider _jwtProvider;
        /*
        CONSTRUCTOR EXPLANATION:
        
        1. WHERE IS IT DECLARED?
           - RIGHT HERE! This code defines how to build an AuthService.
           - It "declares" that to make one, you MUST provide a Context, a Hasher, and a Provider.

        2. WHERE IS IT USED?
           - It is used AUTOMATICALLY by the system (The Dependency Injection Container).
           
        THE CHAIN REACTION:
           A. The USER requests "POST /api/auth/login".
           B. The CONTROLER (AuthController) says: "I need an IAuthService to work!"
           C. The PROGRAM (Program.cs) sees the rule: "If someone needs IAuthService, give them an AuthService."
           D. The SYSTEM looks at this constructor and auto-fills the 3 missing pieces (AppDbContext, etc.).
           E. The SYSTEM runs "new AuthService(...)" and gives it to the Controller.
        */
        public AuthService(AppDbContext context, IPasswordHasher passwordHasher, IJwtProvider jwtProvider)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
        }
        /*
        EXPLANATION OF THE SIGNATURE:
        public async Task<string> Login(LoginDto loginDto)

        1. public: 
           - "Open for Business". The Controller is allowed to call this.

        2. async: 
           - "Don't Freeze". It tells the computer: "This might take a moment (talking to the database), 
             so go ahead and handle other users while I wait."

        3. Task<string>: 
           - "The Promise". It returns a "Task" (a job in progress).
           - When the task finishes, it will give you a "string" (The JWT Token).

        4. (LoginDto loginDto): 
           - "The Input Package". Instead of asking for (string email, string password), 
             we allow the controller to pass a neat box (DTO) containing both.
        */
        public async Task<string> Login(LoginDto loginDto)
        {


            //EXPLANATION OF THE DATABASE QUERY:
            //var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            //1. _context.Users: 
            /* - "Go to the Database". Uses the AppDbContext to access the "Users" table.

          2. FirstOrDefaultAsync(...): 
             - "Find One or Nothing". It tries to find the FIRST match.
             - if it finds one, it returns the User.
             - If it finds NOTHING, it returns "null".
             - "Async" means it won't freeze the app while searching.
             IU 

          3. (u => u.Email == loginDto.Email): 
             - "The Search Filter". This is a Lambda Expression.
             - It reads as: "Check every user 'u'. Is 'u.Email' equal to the 'Email' they typed in?"
          */
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !_passwordHasher.Verify(loginDto.Password, user.Password))
            {
                throw new Exception("Invalid credentials");
            }


            /*
            EXPLANATION OF THE RETURN:
            return _jwtProvider.Generate(user);

            1. _jwtProvider.Generate(user):
               - "Make the Ticket". We hand the User object to the JwtProvider.
               - It packs the User's ID and Role into a secure Token string (JWT).

            2. return:
               - "Hand it over". We send this Token string back to the Controller.
               - The Controller will then send it to the frontend (React/Angular/Vue).
            */
            return _jwtProvider.Generate(user);
        }

        public async Task<User> Register(RegisterDto registerDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                throw new Exception("Email already exists");
            }

            var user = new User
            {
                Nombre = registerDto.Nombre,
                Email = registerDto.Email,
                Password = _passwordHasher.Hash(registerDto.Password),
                Rol = registerDto.Rol
            };

            /*
            EXPLANATION OF SAVING TO DATABASE:

            1. _context.Users.Add(user):
               - "Stage the Changes". This doesn't send anything to the database YET.
               - It tells the Context: "Hey, I have a new user here. Please track it and plan to insert it."
               - Think of it like adding an item to your shopping cart. It's in the cart, but not bought yet.

            2. await _context.SaveChangesAsync():
               - "Commit the Transaction". This is the command that actually talks to the Database.
               - It gathers all the changes (the added user) and runs the SQL "INSERT INTO Users..." command.
               - "Async" means we don't freeze the server while waiting for the database to finish writing.
            */
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}
