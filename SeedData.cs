using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NarodnyUniversity.Data;
using System;
using System.Linq;

namespace NarodnyUniversity.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                context.Database.EnsureCreated();

                // first create roles and user(s)
                //CreateRolesAndUsers(context);

                // Look for any movies.
                if (context.Person.Any())
                {
                    return;   // DB has been seeded
                }

                context.Person.AddRange(
                    new Person
                    {
                        Username = "9633311718",
                        LastName = "Шишкин",
                        FirstName = "Алексей",
                        MiddleName = "Анатольевич",
                        IsStudent = true
                    },
                    new Person
                    {
                        Username = "9116207809",
                        LastName = "Шишкина",
                        FirstName = "Екатерина",
                        MiddleName = "Константиновна",
                        IsInstructor = true
                    },
                    new Person
                    {
                        Username = "9000000000",
                        LastName = "Баклажанчиков-Грушевский",
                        FirstName = "Василий",
                        MiddleName = "Тихонович",
                        IsStudent = true,
                        IsInstructor = true
                    }
                );

                context.SaveChanges();

                var instructors = from i in context.Person
                                  where i.IsInstructor
                                  select i;

                context.Group.AddRange(
                    new Group
                    {
                        Description = "B1"//, // #temp
                        //Instructor = instructors.Single(i => i.LastName == "Шишкина")
                    }
                );

                context.SaveChanges();

            }
        }

        // In this method we will create default User roles and Admin user for login   
        private static async void CreateRolesAndUsers(ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context),null,null,null,null,null);
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context), null, null, null, null, null, null, null, null);


            // create roles
            // creating Creating Employee role    
            if (!await roleManager.RoleExistsAsync("Administrator"))
            {
                var role = new IdentityRole();
                role.Name = "Administrator";
                await roleManager.CreateAsync(role);
            }

            if (!await roleManager.RoleExistsAsync("Manager"))
            {
                var role = new IdentityRole();
                role.Name = "Manager";
                await roleManager.CreateAsync(role);
            }

            if (!await roleManager.RoleExistsAsync("Instructor"))
            {
                var role = new IdentityRole();
                role.Name = "Instructor";
                await roleManager.CreateAsync(role);
            }

            // now let's create Super User
            var user = new ApplicationUser();
            user.UserName = "9633311718";
            user.Email = "forjob_box@yahoo.com";

            string userPWD = "123";

            var chkUser = await userManager.CreateAsync(user, userPWD);

            //Add default User to Role Admin   
            if (chkUser.Succeeded)
            {
                var result1 = await userManager.AddToRoleAsync(user, "Administrator");
            }
        }
    }
}
