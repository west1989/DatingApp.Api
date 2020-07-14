using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.Api.Helpers
{
  public class MessageParams : BaseParams
  {
    public string MessageContainer { get; set; } = "Unread";
  }
}
