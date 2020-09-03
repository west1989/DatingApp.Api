using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.Api.Data;
using DatingApp.Api.Dtos;
using DatingApp.Api.Helpers;
using DatingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DatingApp.Api.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AdminController : ControllerBase
  {
    private readonly DataContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
    private Cloudinary _cloudinary;

    public AdminController(DataContext context, UserManager<User> userManager, IOptions<CloudinarySettings> cloudinaryConfig)
    {
      _context = context;
      _userManager = userManager;
      _cloudinaryConfig = cloudinaryConfig;

      var acc = new Account(_cloudinaryConfig.Value.CloudName, _cloudinaryConfig.Value.ApiKey, _cloudinaryConfig.Value.ApiSecret);

      _cloudinary = new Cloudinary(acc);
    }

    [Authorize(Policy = "RequiredAdminRole")]
    [HttpGet("usersWithRoles")]

    public async Task<IActionResult> GetUsersWithRoles()
    {
      var userList = await _context.Users.
        OrderBy(o => o.UserName)
        .Select(s => new
        {
          Id = s.Id,
          UserName = s.UserName,
          Roles = (from userRole in s.UserRoles
                   join role in _context.Roles on userRole.RoleId equals role.Id
                   select role.Name).ToList()
        }).ToListAsync();

      return Ok(userList);
    }

    [Authorize(Policy = "RequiredAdminRole")]
    [HttpPost("editRoles/{userName}")]
    public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
    {
      var user = await _userManager.FindByNameAsync(userName);

      var userRoles = await _userManager.GetRolesAsync(user);

      var selectedRoles = roleEditDto.RoleNames;

      selectedRoles = selectedRoles ?? new string[] { };

      var addRoles = selectedRoles.Except(userRoles).ToList();

      var result = await _userManager.AddToRolesAsync(user, addRoles);

      if (!result.Succeeded)
        return BadRequest("Failed to add to roles");

      var removeRoles = userRoles.Except(selectedRoles).ToList();

      result = await _userManager.RemoveFromRolesAsync(user, removeRoles);

      if (!result.Succeeded)
        return BadRequest("Failed to remove the roles");

      var rolesResult = await _userManager.GetRolesAsync(user);

      return Ok(rolesResult);
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photosForModeration")]
    public async Task<IActionResult> GetPhotosForModeration()
    {
      var photos = await _context.Photos
        .Include(u => u.User)
        .IgnoreQueryFilters()
        .Where(p => p.IsApproved == false)
        .Select(s => new
        {
          s.Id,
          s.User.UserName,
          s.Url,
          s.IsApproved
        })
        .ToListAsync();

      return Ok(photos);
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("approvePhoto/{photoId}")]
    public async Task<IActionResult> ApprovePhoto(int photoId)
    {
      var photo = await _context.Photos
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(p => p.Id == photoId);

      photo.IsApproved = true;

      await _context.SaveChangesAsync();

      return Ok();
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("rejectPhoto/{photoId}")]
    public async Task<IActionResult> RejectPhoto(int photoId)
    {
      var photo = await _context.Photos
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(p => p.Id == photoId);

      if (photo.IsMain)
        return BadRequest("You cannot reject this main photo");

      if (photo.PublicId != null)
      {
        var deleteParams = new DeletionParams(photo.PublicId);

        var result = await _cloudinary.DestroyAsync(deleteParams);

        if (result.Result == "ok")
        {
          _context.Photos.Remove(photo);
        }
      }

      if (photo.PublicId == null)
      {
        _context.Photos.Remove(photo);
      }

      await _context.SaveChangesAsync();

      return Ok();
    }

  }
}
