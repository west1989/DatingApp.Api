﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.Api.Data;
using DatingApp.Api.Dtos;
using DatingApp.Api.Helpers;
using DatingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.Api.Controllers
{
  [ServiceFilter(typeof(LogUserActivity))]
  [Authorize]
  [Route("api/users/{userId}/[controller]")]
  [ApiController]
  public class MessagesController : ControllerBase
  {
    private readonly IDatingRepository _repo;
    private readonly IMapper _mapper;

    public MessagesController(IDatingRepository repo, IMapper mapper)
    {
      _repo = repo;
      _mapper = mapper;
    }

    [HttpGet("{id}", Name = nameof(GetMessage))]
    public async Task<IActionResult> GetMessage(int userId, int id)
    {
      string userClaimId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
      if (userId != int.Parse(userClaimId))
        return Unauthorized();

      var messageFromRepo = await _repo.GetMessage(id);

      if (messageFromRepo == null)
        return NotFound();

      return Ok(messageFromRepo);
    }

    [HttpGet]
    public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery] MessageParams messageParams)
    {
      string userClaimId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
      if (userId != int.Parse(userClaimId))
        return Unauthorized();

      messageParams.UserId = userId;
      var messagesFromRepo = await _repo.GetMessagesForUser(messageParams);
      var messages = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

      Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize, messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

      return Ok(messages);
    }

    [HttpGet("thread/{recipientId}")]
    public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
    {
      string userClaimId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
      if (userId != int.Parse(userClaimId))
        return Unauthorized();

      var messagesFromRepo = await _repo.GetMessageThread(userId, recipientId);
      var messageThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

      return Ok(messageThread);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
    {

      var sender = await _repo.GetUser(userId);

      string userClaimId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
      if (sender.Id != int.Parse(userClaimId))
        return Unauthorized();

      messageForCreationDto.SenderId = userId;
      var recipient = _repo.GetUser(messageForCreationDto.RecipientId);

      if (recipient == null)
        return BadRequest("Could not find user");

      var message = _mapper.Map<Message>(messageForCreationDto);

      _repo.Add(message);

      if (await _repo.SaveAll())
      {
        var messageToReturn = _mapper.Map<MessageToReturnDto>(message);
        return CreatedAtRoute(nameof(GetMessage), new { userId, id = message.Id }, messageToReturn);
      }

      throw new Exception("Creating message failed on save");
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> DeleteMessage(int id, int userId)
    {
      string userClaimId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
      if (userId != int.Parse(userClaimId))
        return Unauthorized();

      var messageFromRepo = await _repo.GetMessage(id);

      if (messageFromRepo.SenderId == userId)
        messageFromRepo.SenderDeleted = true;

      if (messageFromRepo.RecipientId == userId)
        messageFromRepo.RecipientDeleted = true;

      if (messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
        _repo.Delete(messageFromRepo);

      if (await _repo.SaveAll())
        return NoContent();

      throw new Exception("Error deleting the message");
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkMessageAsRead(int id, int userId)
    {
      string userClaimId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
      if (userId != int.Parse(userClaimId))
        return Unauthorized();

      var message = await _repo.GetMessage(id);

      if (message.RecipientId != userId)
        return Unauthorized();

      message.IsRead = true;
      message.DateRead = DateTime.Now;

      await _repo.SaveAll();

      return NoContent();
    }


  }
}
