using api.Domain;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    //This is a class that inherits from DBContext. So i can use
    //some of the methods and properties of DBContext.
    public class AppDbContext : DbContext
    {
        /*
        CONSTRUCTOR EXPLANATION:
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)

        1. DbContextOptions<AppDbContext> options:
           - "The Configuration Box". This box contains the instructions created in Program.cs.
           - It holds the "Connection String" (Password, URL) and the Database Provider (PostgreSQL).

        2. : base(options):
           - "Pass it to the Parent". 
           - AppDbContext inherits from DbContext (The Microsoft Class).
           - The parent (DbContext) NEEDS those settings to know how to start up.
           - So we take the box we received and immediately hand it up to the parent.
        */

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        /*
        SUMMARY EXPLANATION:
        public DbSet<User> Users { get; set; }
         * 
         * 1. DbSet<User>:
         *    - Represents the "Users" table in your database.
         *    - Acts as a gateway/collection to specific data. 
         *    - Works like a List<User>: You can Add(), Remove(), and get items (ToList()).
         *    - When you call methods on this, EF Core translates them to SQL (INSERT, DELETE, SELECT).
         * 
         * 2. Users:
         *    - The name of this property becomes the table name in the database.
         * 
         * 3. { get; set; }:
         *    - Auto-property accessors allow Entity Framework to initialize this property automatically at runtime.
         */
        public DbSet<User> Users { get; set; }
        /*
         * SUMMARY EXPLANATION:
         * protected override void OnModelCreating(ModelBuilder modelBuilder)
         * 
         * 1. Purpose:
         *    - Configure the database schema (tables, columns, relationships) using the "Fluent API".
         *    - Runs ONCE when the application starts (first time DbContext is used).
         * 
         * 2. ModelBuilder:
         *    - The tool to define how C# classes map to database tables.
         *    - Used for advanced settings like Unique Indexes, Composite Keys, etc.
         * 
         * 3. Why Override?:
         *    - EF Core guesses structure by default. Override this to give specific instructions
         *      that attributes (like [Key]) cannot express.
         */
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
             * IMPLEMENTATION EXPLANATION:
             * 
             * 1. base.OnModelCreating(modelBuilder);
             *    - "Call the implementation of the Parent Class (DbContext) first."
             *    - Why? 
             *      a) Safety: Ensures any default configuration from Microsoft is applied.
             *      b) Inheritance: If you were inheriting from 
                    IdentityDbContext, this line is CRITICAL to set up user 
                    tables. Without it, your auth system breaks.

                    base: This is a special keyword in C# that means "Look at my Parent class". Since your class 
                    AppDbContext inherits from 
                    DbContext
                    , base refers directly to Microsoft's 
                      DbContext
                    .
             */
            base.OnModelCreating(modelBuilder);

            /*
             * 2. builder.Entity<User>().HasIndex(u => u.Email).IsUnique();
             *    - "For the 'User' table..."
             *    - "Create a database INDEX on the 'Email' column."
               (Makes searching by email extremely fast).
             *    - "Make it UNIQUE." (The database will reject any 
                   INSERT/UPDATE if the email already exists).
             *    - This enforces the rule: "No two users can have 
             the same email" at the database level.
             */
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
