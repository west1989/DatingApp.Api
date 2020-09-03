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

    public async Task<Like> GetLike(int userId, int recipientId)
    {
      return await _context.Likes.FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
    }

    public async Task<Photo> GetMainPhotoForUser(int userId)
    {
      return await _context.Photos.Where(w => w.UserId == userId).FirstOrDefaultAsync(f => f.IsMain);
    }

    public async Task<Message> GetMessage(int id)
    {
      return await _context.Messages.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
    {
      var messages = _context.Messages.AsQueryable();

      switch (messageParams.MessageContainer)
      {
        case "Inbox":
          messages = messages.Where(w => w.RecipientId == messageParams.UserId && !w.RecipientDeleted);
          break;
        case "Outbox":
          messages = messages.Where(w => w.SenderId == messageParams.UserId && !w.SenderDeleted);
          break;
        default:
          messages = messages.Where(w => w.RecipientId == messageParams.UserId && !w.RecipientDeleted && !w.IsRead);
          break;
      }

      messages = messages.OrderByDescending(o => o.MessageSent);

      var pagedMessages = await PagedList<Message>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);

      return pagedMessages;
    }


    public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
    {
      var messages = await _context.Messages
        .Where(w => w.RecipientId == userId && !w.RecipientDeleted && w.SenderId == recipientId
        || w.RecipientId == recipientId && w.SenderId == userId && !w.SenderDeleted)
        .OrderByDescending(o => o.MessageSent)
        .ToListAsync();

      return messages;
    }

    public async Task<Photo> GetPhoto(int id)
    {
      var photo = await _context.Photos
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(f => f.Id == id);
      return photo;
    }

    public async Task<User> GetUser(int Id, bool isCurrentUser)
    {
      

      var query =  _context.Users.Include(p => p.Photos).AsQueryable();

      if (isCurrentUser)
        query = query.IgnoreQueryFilters();

      var user = await query.FirstOrDefaultAsync(f => f.Id == Id);

      return user;
    }

    public async Task<PagedList<User>> GetUsers(UserParams userParams)
    {
      var users = _context.Users.OrderByDescending(o => o.LastActive).AsQueryable();

      users = users.Where(w => w.Id != userParams.UserId);
      users = users.Where(w => w.Gender == userParams.Gender || string.IsNullOrEmpty(userParams.Gender));

      if (userParams.Likers)
      {
        var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
        users = users.Where(w => userLikers.Contains(w.Id));
      }

      if (userParams.Likees)
      {
        var userLikees = await GetUserLikes(userParams.UserId, userParams.Likers);
        users = users.Where(w => userLikees.Contains(w.Id));
      }

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

    private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers)
    {
      var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

      if (likers)
      {
        return user.Likers.Where(w => w.LikeeId == id).Select(s => s.LikerId);
      }
      else
      {
        return user.Likees.Where(w => w.LikerId == id).Select(s => s.LikeeId);
      }
    }
  }
}
