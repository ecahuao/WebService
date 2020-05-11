using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApplication.Data;
using WebApplication.Models;
using Microsoft.AspNetCore.Http.Internal;
using System.IO;
using System.Text;

namespace WebApplication
{
    public class Startup
    {
        private IHostingEnvironment xenvironment;
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            xenvironment = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddDbContext<Context>(options =>
            options.UseSqlServer(Configuration.GetValue<string>("Context")));
            services.AddScoped<dataRepository>();
            services.AddMvc()
                    .AddXmlSerializerFormatters();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.Run(MyMiddleware);
            /*app.Run(async context =>
            {
                //await _dataRep.postRepository("1", "1");
                await context.Response.WriteAsync("Hello from non-Map delegate. <p>");
            });*/
            app.UseHttpsRedirection();
            app.UseMvc();
        }
        private async Task MyMiddleware(HttpContext context)
        {
            dataRepository dr = new dataRepository(Configuration, xenvironment);
            var bodyStr = "";
            byte[] data;
            Resp resp = await dr.postRepository(bodyStr);
            var req = context.Request;
            string contentType = context.Request.ContentType;
            req.EnableRewind();
            using (StreamReader reader
                      = new StreamReader(req.Body, Encoding.UTF8, true, 1024, true))
            {
                bodyStr = reader.ReadToEnd();
            }
            req.Body.Position = 0;
            string metod = context.Request.Method;
            string stringHeader = (context.Request.ContentType == null ? "": context.Request.ContentType);
            if (stringHeader.ToLower() == "application/json")
            {
                if ((bodyStr.Trim() != ""))
                {
                    string verbType = context.Request.Method;
                    if (verbType.ToLower() == "post")
                    {
                        resp = await dr.postRepository(bodyStr);
                        context.Response.StatusCode = resp.resp;
                        data = Encoding.UTF8.GetBytes(resp.message);
                        context.Response.ContentType = "application/json";
                        await context.Response.Body.WriteAsync(data, 0, data.Length);
                        return;
                    }
                    else if (verbType.ToLower() == "put")
                    { 

                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        data = Encoding.UTF8.GetBytes("{\"message\": \"El verbo "+ verbType + " no ha sido habilitado en la API \"}");
                        context.Response.ContentType = "application/json";
                        await context.Response.Body.WriteAsync(data, 0, data.Length);
                        return;
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                    data = Encoding.UTF8.GetBytes("{\"message\": \"Debe incluir información en el Body de la petición \"}");
                    context.Response.ContentType = "application/json";
                    await context.Response.Body.WriteAsync(data, 0, data.Length);
                    return;
                }
            }
            else 
            {
                context.Response.StatusCode = 500;
                data = Encoding.UTF8.GetBytes("{\"message\": \"No se aceptan solicitudes " + stringHeader.ToLower() + "\"}");
                context.Response.ContentType = "application/json";
                await context.Response.Body.WriteAsync(data, 0, data.Length);
                return;
            }
        }
    }
}
