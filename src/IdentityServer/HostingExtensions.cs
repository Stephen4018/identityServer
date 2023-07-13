using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Test;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IdentityServer;

internal static class HostingExtensions
{
    //public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    //{
    //    // uncomment if you want to add a UI
    //    builder.Services.AddRazorPages();

    //    builder.Services.AddIdentityServer(options =>
    //        {
    //            // https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/api_scopes#authorization-based-on-scopes
    //            options.EmitStaticAudienceClaim = true;
    //        })
    //        .AddInMemoryIdentityResources(Config.IdentityResources)
    //        .AddInMemoryApiScopes(Config.ApiScopes)
    //        .AddInMemoryClients(Config.Clients);

    //    return builder.Build();
    //}


    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages(); //this adds configurations to use razor pages for UI


        var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        //const string connectionString = @"Server=localhost;Port=5432;Database=IdentityDB;User Id=postgres;password=ayooluwa;";

        var isBuilder = builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;
            })
             //.AddInMemoryIdentityResources(Config.IdentityResources)
             //.AddInMemoryApiScopes(Config.ApiScopes)
             //.AddInMemoryClients(Config.Clients)

        //this adds the config data from DB (clients, resources, CORS)
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
                    sql => sql.MigrationsAssembly(migrationsAssembly));
        })
       .AddOperationalStore(options =>
              {
                  options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
                      sql => sql.MigrationsAssembly(migrationsAssembly));
              })
        .AddTestUsers(TestUsers.Users);

        builder.Services.AddControllersWithViews();
        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                // register your IdentityServer with Google at https://console.developers.google.com
                // enable the Google+ API
                // set the redirect URI to https://localhost:5001/signin-google
                options.ClientId = "copy client ID from Google here";
                options.ClientSecret = "copy client secret from Google here";
            });


        // this adds the necessary config for the simple admin/config pages
        {
            //// ENSURE TO COME BACK TO THIS AND COMPLETE IT
            //builder.Services.AddAuthorization(options =>
            //    options.AddPolicy("admin",
            //        policy => policy.RequireClaim("sub", "1"))
            //);

            //builder.Services.Configure<RazorPagesOptions>(options =>
            //    options.Conventions.AuthorizeFolder("/Admin", "admin"));

            //builder.Services.AddTransient<MCloudStorage.IdentityServer.Pages.Portal.ClientRepository>();
            //builder.Services.AddTransient<ClientRepository>();
            //builder.Services.AddTransient<IdentityScopeRepository>();
            //builder.Services.AddTransient<ApiScopeRepository>();
        }

        // if you want to use server-side sessions: https://blog.duendesoftware.com/posts/20220406_session_management/
        // then enable it
        //isBuilder.AddServerSideSessions();
        //
        // and put some authorization on the admin/management pages using the same policy created above
        //builder.Services.Configure<RazorPagesOptions>(options =>
        //    options.Conventions.AuthorizeFolder("/ServerSideSessions", "admin"));

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    { 
        app.UseSerilogRequestLogging();
    
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        InitializeDatabase(app);

        // uncomment if you want to add a UI
        app.UseStaticFiles();
        app.UseRouting();

        app.UseIdentityServer();

        // uncomment if you want to add a UI
        app.UseAuthorization();
        app.MapRazorPages().RequireAuthorization();

        return app;
    }

    private static void InitializeDatabase(IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
        {
            serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Database.Migrate();
            if (!context.Clients.Any())
            {
                foreach (var client in Config.Clients)
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in Config.IdentityResources)
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.ApiScopes.Any())
            {
                foreach (var resource in Config.ApiScopes)
                {
                    context.ApiScopes.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }
        }
    }
}
