using AgileManagement.Application;
using AgileManagement.Application.services;
using AgileManagement.Application.validators;
using AgileManagement.Core;
using AgileManagement.Core.validation;
using AgileManagement.Domain;
using AgileManagement.Domain.events;
using AgileManagement.Domain.handler;
using AgileManagement.Domain.repositories;
using AgileManagement.Infrastructure.events;
using AgileManagement.Infrastructure.notification.smtp;
using AgileManagement.Infrastructure.security.hash;
using AgileManagement.Persistence.EF;
using AgileManagement.Persistence.EF.repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgileManagement.Mvc
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
           
            
          

            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddHttpContextAccessor(); // IHttpContext Accessor
            services.AddDataProtection(); // Uygulamada dataProtection �zelli�i kullanaca��m.

            // Mvc uygulamas�nda automapper kullanaca��m�z� s�yledik
            services.AddAutoMapper(typeof(Startup));

            // konfig�rasyon, yard�mc� servis gibi tek instance ile �al��abilen yap�lar i�in singleton tercih edelim
            services.AddSingleton<IEmailService, NetSmtpEmailService>();
            services.AddTransient<IUserRegisterValidator, UserRegisterValidator>();
            // validation, session i�lemleri i�in transient tercih edelim
           

            // veri taban� , servis �a��r�s�, api �a��r�s� gibi i�lemler i�in scoped tercih edelim
            services.AddSingleton<IPasswordHasher, CustomPasswordHashService>();
            services.AddScoped<ICookieAuthenticationService, CookieAuthenticationService>();
            services.AddScoped<IUserRegisterService, UserRegisterService>();
            services.AddScoped<IAccountVerifyService, AccountVerifyService>();
            services.AddScoped<IUserLoginService, UserLoginService>();
            services.AddScoped<IUserDomainService, UserDomainService>();
            services.AddScoped<IUserRepository, EFUserRepository>();
            services.AddScoped<IProjectRepository, EFProjectRepository>();
            // best practice olarak db context uygyulamas� appsettings dosyas�ndan bilgileri conectionstrings node dan al�r�z.



            //services.AddSingleton<IDomainEventDispatcher, NetCoreEventDispatcher>();


            //services.AddAuthentication("SecureScheme").AddCookie("SecureScheme", opt =>
            //{
            //    opt.Cookie.HttpOnly = false; // https bir cookie ile cookie https protocol� ile �al��s�n
            //    opt.Cookie.Name = "AdminCookie";
            //    opt.ExpireTimeSpan = TimeSpan.FromDays(1); // 1 g�nl�k olarak cookie browserdan silinmeyecek
            //    opt.LoginPath = "/Admin/Accoun/Login";
            //    opt.LogoutPath = "/Admin/Account/Logout";
            //    opt.AccessDeniedPath = "/Admin/Account/AccessDenied"; // yetkiniz olmayan sayfalar.
            //});

            // NormalAuth bizim uygulamdaki normal kullan�c�lar i�in a�t���m�z kimlik do�rulama �emas�d�r.
            services.AddAuthentication("NormalScheme").AddCookie("NormalScheme", opt =>
             {

                 opt.Cookie.HttpOnly = false; // https bir cookie ile cookie https protocol� ile �al��s�n
                opt.Cookie.Name = "NormalCookie";
                 opt.LoginPath = "/Account/Login";
                 opt.LogoutPath = "/Account/Logout";
                 opt.AccessDeniedPath = "/Account/AccessDenied"; // yetkiniz olmayan sayfalar.
                opt.SlidingExpiration = true; // otomatik olarak cookie yenileme, s�resini kayd�rarak expire time yeniden 30 g�n sonras�na atar.
                                              // cookie expire olunca tekrar login olmam�z gerekiyor.

            });


          

            // Y�netim paneline giri� yetkisi olan kullan�lar i�in olucak olan cookie




            services.AddDbContext<UserDbContext>(opt =>
            {
                opt.UseSqlServer(Configuration.GetConnectionString("LocalDb"));
            });

            services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlServer(Configuration.GetConnectionString("LocalDb"));
            });


            IKernel kernel = new StandardKernel();
            NinjectEventModule.RegisterServices(kernel);

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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            // bunun yeri �enmli UseRouting ile UseAuthorization aras�na konumland�ral�m.
            app.UseAuthentication(); // sistemde kimlik do�rulamas� var kullan�c�n�n hesab�n� cookie �zerinden kontrol ederiz.
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
