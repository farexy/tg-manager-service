using k8s;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TG.Core.App.Configuration;
using TG.Core.App.InternalCalls;
using TG.Core.App.Middlewares;
using TG.Core.App.Swagger;
using TG.Core.Db.Postgres;
using TG.Manager.Service.Config;
using TG.Manager.Service.Config.Options;
using TG.Manager.Service.Db;
using TG.Manager.Service.Extensions;
using TG.Manager.Service.ServiceClients;
using TG.Manager.Service.Services;

namespace TG.Manager.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddTgJsonOptions()
                .AddInvalidModelStateHandling(); 
            services.AddHealthChecks();
            //.AddNpgSqlHealthCheck();
            //services.AddKubernetesTgApplicationInsights(Configuration);
            services.AddApiVersioning();
            
            services.AddPostgresDb<ApplicationDbContext>(Configuration, ServiceConst.ServiceName);

            services.AddAutoMapper<Startup>();
            services.AddMediatR(typeof(Startup));
                
            services.AddTgServices();

            services.Configure<PortsRange>(Configuration.GetSection(nameof(PortsRange)));

            services.ConfigureInternalCalls(Configuration);
            services.AddServiceClient<IConfigsClient>(Configuration.GetConfigsUrl());

            services.AddTgSwagger(opt =>
            {
                opt.ServiceName = ServiceConst.ServiceName;
                opt.ProjectName = ServiceConst.ProjectName;
                opt.AppVersion = "1";
            });

            services.AddSingleton<IRealtimeServerDeploymentConfigProvider, RealtimeServerDeploymentConfigProvider>();

            services.AddSingleton<IKubernetes>(new Kubernetes(Environment.GetK8sConfig()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseTgSwagger();

            app.UseCors();

            app.UseRouting();

            app.UseMiddleware<TracingMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
                endpoints.MapReloadDeploymentConfig();
            });
        }
    }
}
