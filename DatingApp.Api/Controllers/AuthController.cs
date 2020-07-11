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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.Api.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AuthController : ControllerBase
  {
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;

    public AuthController(IAuthRepository authRepository, IConfiguration config, IMapper mapper)
    {
      _authRepository = authRepository;
      _config = config;
      _mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
    {
      userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

      if (await _authRepository.UserExists(userForRegisterDto.Username))
        return BadRequest("Username already exists");

      var userToCreate = new User
      {
        Username = userForRegisterDto.Username
      };

      var createdUser = await _authRepository.Register(userToCreate, userForRegisterDto.Password);

      return StatusCode(201);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
    {
      var userRepo = await _authRepository.Login(userForLoginDto.Username, userForLoginDto.Password);      

      if (userRepo == null)
        return Unauthorized();      

      var claims = new Claim[]
      {
        new Claim(ClaimTypes.NameIdentifier, userRepo.Id.ToString()),
        new Claim(ClaimTypes.Name, userRepo.Username)
      };

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
      var user = _mapper.Map<UserForListDto>(userRepo);

      return Ok(new 
      { 
        token = tokenHandler.WriteToken(token),
        user
      });
    }

  }
}
