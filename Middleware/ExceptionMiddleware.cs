using System.Net;
using System.Text.Json;

namespace api.Middleware
{
    public class ExceptionMiddleware
    {
        /*
         * EXPLANATION: The "Next" Step
         * 
         * 1. RequestDelegate:
         *    - A function that can process an HTTP request.
         * 
         * 2. _next:
         *    - This variable holds the reference to the NEXT middleware in the pipeline.
         *    - When we call "await _next(context);", we pass the baton to the next runner.
         *    - If we don't call it, the request stops here.
         */
        private readonly RequestDelegate _next;
        /*
         * EXPLANATION: The "Reporter"
         * 
         * 1. ILogger:
         *    - This is the application's diary/journal.
         *    - It records events (Info, Warnings, Errors).
         * 
         * 2. <ExceptionMiddleware>:
         *    - This is the Category or Tag.
         *    - When we look at the logs, we will see "ExceptionMiddleware: User login failed".
         *    - This helps us filter logs to find exactly where an error happened.
         */
        private readonly ILogger<ExceptionMiddleware> _logger;
        /*
         * EXPLANATION: The "Environment Check"
         * 
         * 1. Purpose:
         *    - Tells us WHERE the code is running (Development vs Production).
         * 
         * 2. Why we need it:
         *    - In 'Development' (your PC), we want to see the full error (Stack Trace) to fix bugs.
         *    - In 'Production' (Live Server), we hide the details for security.
         *    - This variable lets us check "Are we in Development?" before showing the error.
         */
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        /*
         * SUMMARY EXPLANATION:
         * 
         * 1. Purpose:
         *    - This is the "Safety Net" for the entire application.
         *    - It wraps EVERY request in a big try-catch block.
         * 
         * 2. Logic (InvokeAsync):
         *    - await _next(context): "Go run the rest of the app (Controllers, DB, etc)."
         *    - If everything is fine, it does nothing.
         *    - If ANY error happens anywhere (Exception ex), it catches it here.
         * 
         * 3. The Response:
         *    - Instead of crashing or showing a "Yellow Screen of Death", it returns JSON.
         *    - Development: Shows exact error & stack trace (so you can fix it).
         *    - Production: Shows generic "Internal Server Error" (so hackers don't see code).
         */

        /*
        Who calls it? The ASP.NET Core Request Pipeline (Internal 
        code of the framework).
        When is it called? Every single time an HTTP request 
        hits your server.
        How does the framework know which method to call? It 
        searches your class for a method with the exact signature 
        public async Task InvokeAsync(HttpContext context).

        */
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {

                /*
                Passes the Request Forward: It tells the system: "I have 
                finished my part (setting up the safety net). 
                Now, call the next middleware in the line."

                
                */
                await _next(context);
            }
            catch (Exception ex)
            {

                /*
                If ANY code that happens after this line (like a bug in 
                your AuthService or a database error) throws an exception,
                 the execution "ball" is dropped.



                */
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                /*
                 * EXPLANATION: The "Ternary Operator" ( ? : )
                 * 
                 * This is a shortcut for an if-else statement.
                 * Format: condition ? value_if_true : value_if_false
                 * 
                 * 1. Condition: _env.IsDevelopment()
                 *    - "Are we on the developer's computer?"
                 * 
                 * 2. If TRUE (Development):
                 *    - Returns: new { message = ex.Message, stackTrace = ex.StackTrace }
                 *    - Why: We give the full details (Message + Stack Trace) so the developer can debug.
                 * 
                 * 3. If FALSE (Production/Live):
                 *    - Returns: new { message = "Internal Server Error" }
                 *    - Why: We give a generic message. Security rule: NEVER show stack traces to the public.
                 */
                object response = _env.IsDevelopment()
                    ? new { message = ex.Message, stackTrace = ex.StackTrace }
                    : new { message = "Internal Server Error" };


                /*
                 * EXPLANATION: JSON Formatting
                 * 
                 * 1. JsonSerializerOptions:
                 *    - Configures how the C# object is converted to text.
                 * 
                 * 2. PropertyNamingPolicy = JsonNamingPolicy.CamelCase:
                 *    - Standard convention for JSON APIs.
                 *    - C# properties are usually PascalCase (StackTrace).
                 *    - JSON fields are usually camelCase (stackTrace).
                 *    - This automatically converts them.
                 * 
                 * 3. JsonSerializer.Serialize:
                 *    - Takes the C# object (response) and turns it into a JSON string.
                 */
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);

                /*
                 * EXPLANATION: Sending the Response
                 * 
                 * 1. context.Response:
                 *    - The empty box that will be shipped back to the user/frontend.
                 * 
                 * 2. WriteAsync(json):
                 *    - Puts our JSON string inside the box.
                 *    - This is the final step. The middleware pipeline ends here for this request.
                 *    - The user receives the JSON error message on their screen.
                 */
                await context.Response.WriteAsync(json);
            }
        }
    }
}
