using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SCIM.WebHostSample.Provider;

namespace Microsoft.SCIM.WebHostSample
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public IMonitor MonitoringBehavior { get; set; }
        public IProvider ProviderBehavior { get; set; }

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this._env = env;
            this._configuration = configuration;

            this.MonitoringBehavior = new ConsoleMonitor();
            this.ProviderBehavior = new InMemoryProvider();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            if (_env.IsDevelopment())
            {
                // Development environment code
                // Validation for bearer token for authorization used during testing.
                // This is not meant to replace proper OAuth for authentication purposes.
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters =
                            new TokenValidationParameters
                            {
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = false,
                                ValidateIssuerSigningKey = false,
                                ValidIssuer = this._configuration["Token:TokenIssuer"],
                                ValidAudience = this._configuration["Token:TokenAudience"],
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this._configuration["Token:IssuerSigningKey"]))
                            };
                    });
            }
            else
            {
                // Azure AD token validation code
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                    .AddJwtBearer(options =>
                    {
                        options.Authority = this._configuration["Token:TokenIssuer"];
                        options.Audience = this._configuration["Token:TokenAudience"];
                        options.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = context =>
                            {
                                // NOTE: You can optionally take action when the OAuth 2.0 bearer token was validated.

                                return Task.CompletedTask;
                            },
                            OnAuthenticationFailed = AuthenticationFailed
                        };
                    });
            }

            services.AddControllers().AddNewtonsoftJson();

            services.AddSingleton(typeof(IProvider), this.ProviderBehavior);
            services.AddSingleton(typeof(IMonitor), this.MonitoringBehavior);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHsts();

            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(
                (IEndpointRouteBuilder endpoints) =>
                {
                    endpoints.MapDefaultControllerRoute();
                });
        }

        private Task AuthenticationFailed(AuthenticationFailedContext arg)
        {
            // For debugging purposes only!
            var s = $"{{AuthenticationFailed: '{arg.Exception.Message}'}}";

            arg.Response.ContentLength = s.Length;
            arg.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(s), 0, s.Length);

            return Task.FromException(arg.Exception);
        }
    }
}
