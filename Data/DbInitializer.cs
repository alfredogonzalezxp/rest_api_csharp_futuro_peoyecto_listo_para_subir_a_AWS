/*
so this // Ensure database is created (without migrations)
context.Database.EnsureCreated();
and DbInitializer.Initialize(context, passwordHasher); 
this is for create the table 

so the table users and his fields is taken for domain.user isnt?

*/



using api.Common;
using api.Domain;

namespace api.Data
{
    public static class DbInitializer
    {
        /*
         * SUMMARY EXPLANATION: Database Seeding Logic
         * 
         * 1. Purpose: 
         *    - Populates the database with initial data (like an Admin user) 
         *      so the system is usable immediately after it's created.
         * 
         * 2. Frequency (The "Only One Time" Rule):
         *    - This is NOT "per user". It's only used ONE TIME for the whole database.
         *    - It checks if ANY users exist. If even one user is found, it stops immediately.
         *    - This ensures that your real data is never overwritten or duplicated.
         */
        public static void Initialize(AppDbContext context, IPasswordHasher passwordHasher)
        {
            // Ensure the database is created (or migrated)
            // context.Database.EnsureCreated(); // Or use migrations separately

            // Look for any users.
            if (context.Users.Any())
            {
                return;   // DB has been seeded
            }

            var users = new User[]
            {
                new User
                {
                    Nombre = "Admin System",
                    Email = "admin@system.com",
                    Password = passwordHasher.Hash("Admin123!"),
                    Rol = "Admin"
                }
            };

            foreach (var u in users)
            {
                context.Users.Add(u);
            }
            context.SaveChanges();
        }
    }
}
