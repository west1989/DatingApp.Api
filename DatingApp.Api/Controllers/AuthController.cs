using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.Api.Data;
using DatingApp.Api.Dtos;
using DatingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.Api.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [AllowAnonymous]
  public class AuthController : ControllerBase
  {
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AuthController(IConfiguration config, IMapper mapper, UserManager<User> userManager, SignInManager<User> signInManager)
    {      
      _config = config;
      _mapper = mapper;
      _userManager = userManager;
      _signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
    {
      //userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

      //if (await _authRepository.UserExists(userForRegisterDto.Username))
      //  return BadRequest("Username already exists");

      var userToCreate = _mapper.Map<User>(userForRegisterDto);

      
      var result = await _userManager.CreateAsync(userToCreate, userForRegisterDto.Password);

      // var createdUser = await _authRepository.Register(userToCreate, userForRegisterDto.Password);

      var userToTeturn = _mapper.Map<UserForDetailedDto>(userToCreate);

      if (result.Succeeded)
      {
        return CreatedAtRoute("GetUser", new { controller = "Users", id = userToCreate.Id }, userToTeturn);
      }

      var errors = string.Empty;

      foreach (var error in result.Errors)
      {
        errors += error.Description;
      }

      return BadRequest(errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
    {
      //var userRepo = await _authRepository.Login(userForLoginDto.Username, userForLoginDto.Password);
      var user = await _userManager.FindByNameAsync(userForLoginDto.Username);

      var result = await _signInManager.CheckPasswordSignInAsync(user, userForLoginDto.Password, false);

      if (result.Succeeded)
      {
        var appUser = _mapper.Map<UserForListDto>(user);
        var token = await GenerateJwtToken(user);

        return Ok(new { token, user = appUser });
      }

      return Unauthorized();
    }

    private async Task<string> GenerateJwtToken(User user)
    {
      var claims = new List<Claim>
      {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName)
      };

      var roles = await _userManager.GetRolesAsync(user);

      foreach (var role in roles)
      {
        claims.Add(new Claim(ClaimTypes.Role, role));
      }

      var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.Now.AddDays(1),
        SigningCredentials = creds
      };

      var tokenHandler = new JwtSecurityTokenHandler();
      var token = tokenHandler.CreateToken(tokenDescriptor);


      return tokenHandler.WriteToken(token);
    }

  }
}
