using DatingApp.Api.Helpers;
using DatingApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.Api.Data
{
  public class DatingRepository : IDatingRepository
  {
    private readonly DataContext _context;

    public DatingRepository(DataContext context)
    {
      _context = context;
    }

    public void Add<T>(T entity) where T : class
    {
      _context.Add(entity);
    }

    public void Delete<T>(T entity) where T : class
    {
      _context.Remove(entity);
    }

    public async Task<Photo> GetMainPhotoForUser(int userId)
    {
      return await _context.Photos.Where(w => w.UserId == userId).FirstOrDefaultAsync(f => f.IsMain);
    }

    public async Task<Photo> GetPhoto(int id)
    {
      var photo = await _context.Photos.FirstOrDefaultAsync(f => f.Id == id);
      return photo;
    }

    public async Task<User> GetUser(int Id)
    {
      var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(f => f.Id == Id);

      return user;
    }

    public  async Task<PagedList<User>> GetUsers(UserParams userParams)
    {
      var users = _context.Users.Include(p => p.Photos)
        .OrderByDescending(o => o.LastActive).AsQueryable();

      users = users.Where(w => w.Id != userParams.UserId);
      users = users.Where(w => w.Gender == userParams.Gender);

      if (userParams.MinAge != 18 || userParams.MaxAge != 99)
      {
        var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
        var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

        users = users.Where(w => w.DateOfBirth >= minDob && w.DateOfBirth <= maxDob);
      }

      if (!string.IsNullOrEmpty(userParams.OrderBy))
      {
        switch (userParams.OrderBy)
        {
          case "created":
            users = users.OrderByDescending(o => o.Created);
            break;
          default:
            users = users.OrderByDescending(o => o.LastActive);
            break;
        }
      }

      var pageListUsers = await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);

      return pageListUsers;
    }

    public async Task<bool> SaveAll()
    {
      return await _context.SaveChangesAsync() > 0;
    }
  }
}
