﻿using DatingApp.Api.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.Api.Data
{
  public class Seed
  {
    //public static void SeedUsers(DataContext context)
    //{
    //  if (!context.Users.Any())
    //  {
    //    var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
    //    var users = JsonConvert.DeserializeObject<List<User>>(userData);

    //    foreach (var user in users)
    //    {
    //      byte[] passwordHash;
    //      byte[] passwordSalt;

    //      CreatePasswordHash("password", out passwordHash, out passwordSalt);

    //      user.PasswordHash = passwordHash;
    //      user.PasswordSalt = passwordSalt;
    //      user.UserName = user.UserName.ToLower();
    //      context.Users.Add(user);
    //    }

    //    context.SaveChanges();
    //  }
    //}

    public static void SeedUsers(UserManager<User> userManager, RoleManager<Role> roleManager)
    {
      if (!userManager.Users.Any())
      {
        var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
        var users = JsonConvert.DeserializeObject<List<User>>(userData);

        //create roles

        var roles = new List<Role>
        {
          new Role {Name = "Member"},
          new Role {Name = "Admin"},
          new Role {Name = "Moderator"},
          new Role {Name = "VIP"}
        };

        foreach (var role in roles)
        {
          roleManager.CreateAsync(role).Wait();
        }

        foreach (var user in users)
        {
          user.Photos.SingleOrDefault().IsApproved = true;
          userManager.CreateAsync(user, "password").Wait();
          userManager.AddToRoleAsync(user, "Member").Wait();
        }

        var adminUser = new User
        {
          UserName = "Admin"
        };

        var result = userManager.CreateAsync(adminUser, "1234").Result;

        if (result.Succeeded)
        {
          var admin = userManager.FindByNameAsync("Admin").Result;
          userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" }).Wait();
        }
      }
    }

    private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512())
      {
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
      }
    }
  }
}
