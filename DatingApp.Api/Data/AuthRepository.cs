using DatingApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.Api.Data
{
  public class AuthRepository : IAuthRepository
  {
    private readonly DataContext _context;

    public AuthRepository(DataContext context)
    {
      _context = context;
    }

    public async Task<User> Login(string username, string password)
    {
      var user = await _context.Users.FirstOrDefaultAsync(f => f.Username == username);

      if (user == null)
        return null;

      if (!VerifyPasswordhash(password, user.PasswordHash, user.PasswordSalt))
        return null;

      return user;
    }

    public async Task<User> Register(User user, string password)
    {
      byte[] passwordHash;
      byte[] passworSalt;

      CreatePasswordHash(password, out passwordHash, out passworSalt);

      user.PasswordHash = passwordHash;
      user.PasswordSalt = passworSalt;

      await _context.Users.AddAsync(user);
      await _context.SaveChangesAsync();

      return user;
    }

    public async Task<bool> UserExists(string username)
    {
      var userExists = await _context.Users.AnyAsync(x => x.Username == username);

      if (userExists)
        return true;

      return false;
    }

    #region private methods

    private bool VerifyPasswordhash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
      {
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        for (int i = 0; i < computedHash.Length; i++)
        {
          if (computedHash[i] != passwordHash[i])
            return false;
        }

        return true;
      }
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passworSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512())
      {
        passworSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
      }
    }

    #endregion

  }

}
