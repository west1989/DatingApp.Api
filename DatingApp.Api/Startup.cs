using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DatingApp.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using DatingApp.Api.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using DatingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace DatingApp.Api
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {


      IdentityBuilder builder = services.AddIdentityCore<User>(opt =>
      {
        opt.Password.RequireDigit = false;
        opt.Password.RequiredLength = 4;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = false;
      });

      builder = new IdentityBuilder(builder.UserType, typeof(Role), builder.Services);
      builder.AddEntityFrameworkStores<DataContext>();
      builder.AddRoleValidator<RoleValidator<Role>>();
      builder.AddRoleManager<RoleManager<Role>>();
      builder.AddSignInManager<SignInManager<User>>();

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetSection("AppSettings:Token").Value)),
          ValidateIssuer = false,
          ValidateAudience = false
        };
      });

      services.AddAuthorization(options => 
      {
        options.AddPolicy("RequiredAdminRole", policy => policy.RequireRole("Admin"));
        options.AddPolicy("ModeratePhotoRole", policy => policy.RequireRole("Admin", "Moderator"));
        options.AddPolicy("VipOnly", policy => policy.RequireRole("VIP"));
      });

      services.AddDbContext<DataContext>(x =>
      {
        x.UseLazyLoadingProxies();
        x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
      });

      services.AddControllers(optiones =>
      {
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        optiones.Filters.Add(new AuthorizeFilter(policy));
      })
      .AddNewtonsoftJson(opt =>
      {
        opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
      });

      services.AddCors();
      services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
      services.AddAutoMapper(typeof(DatingRepository).Assembly);
      services.AddTransient<Seed>();
      //services.AddScoped<IAuthRepository, AuthRepository>();
      services.AddScoped<IDatingRepository, DatingRepository>();

      services.AddScoped<LogUserActivity>();

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {

        app.UseExceptionHandler(builder =>
        {
          builder.Run(async context =>
          {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var error = context.Features.Get<IExceptionHandlerFeature>();
            if (error != null)
            {
              context.Response.ApplicationError(error.Error.Message);
              await context.Response.WriteAsync(error.Error.Message);
            }
          });
        });

      }

      // app.UseHttpsRedirection();

      app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseDefaultFiles();
      app.UseStaticFiles();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
        endpoints.MapFallbackToController("Index", "Fallback");
      });

    }
  }
}
