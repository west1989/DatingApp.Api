﻿using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.Api.Helpers
{
  public class UserParams : BaseParams
  {
    public string Gender { get; set; }
    public int MinAge { get; set; } = 18;
    public int MaxAge { get; set; } = 99;

    public string OrderBy { get; set; }
    public bool Likees { get; set; } = false;
    public bool Likers { get; set; } = false;
  }
}
