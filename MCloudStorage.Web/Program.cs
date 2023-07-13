using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);


var client = new HttpClient();
var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
if (disco.IsError)
{
    Console.WriteLine(disco.Error);
    return;
}

var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
{
    Address = disco.TokenEndpoint,
    ClientId = "interactive",
    ClientSecret = "secret",
    Scope = "scope2"
});

if (tokenResponse.IsError)
{
    Console.WriteLine(tokenResponse.Error);
    Console.WriteLine(tokenResponse.ErrorDescription);
    return;
}

Console.WriteLine(tokenResponse.Json);
Console.WriteLine("\n\n");

// call api
var apiClient = new HttpClient();
apiClient.SetBearerToken(tokenResponse.AccessToken);

var response = await apiClient.GetAsync("https://localhost:6001/identity");
if (!response.IsSuccessStatusCode)
{
    Console.WriteLine(response.StatusCode);
}
else
{
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
}

// Add services to the container.
builder.Services.AddRazorPages();

#region This was added recently
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
    .AddCookie("Cookies")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://localhost:5001";

        options.ClientId = "interactive";
        options.ClientSecret = "secret";
        options.ResponseType = "code";

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");

        options.SaveTokens = true;
    });
#endregion

#region This should be removed but let it be under check

//builder.Services.AddControllersWithViews()
//    .AddJsonOptions(configure =>
//        configure.JsonSerializerOptions.PropertyNamingPolicy = null);

//// create an HttpClient used for accessing the API
//builder.Services.AddHttpClient("APIClient", client =>
//{
//    client.BaseAddress = new Uri(builder.Configuration["MCloudStorageAPIRoot"]);
//    client.DefaultRequestHeaders.Clear();
//    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
//});
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
//}
//).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
//.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
//{
//    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.Authority = "https://localhost:5001";
//    //options.ClientId = "m-cloud-storage-web-clientID";
//    options.ClientId = "interactive";
//    options.ClientSecret = "secret";
//    //options.ClientSecret = "AvQQY7Y8rIiIuTWpKhS7ag==";
//    options.ResponseType = "code";
//    //options.Scope.Add("openid");
//    //options.Scope.Add("profile");
//    //options.CallbackPath = new PathString("signin-oidc");
//    // SignedOutCallbackPath: default = host:port/signout-callback-oidc.
//    // Must match with the post logout redirect URI at IDP client config if
//    // you want to automatically return to the application after logging out
//    // of IdentityServer.
//    // To change, set SignedOutCallbackPath
//    // eg: options.SignedOutCallbackPath = new PathString("pathaftersignout");
//    options.SaveTokens = true;
//    options.GetClaimsFromUserInfoEndpoint = true;
//}
//);
#endregion it ends here

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Mcloud}/{action=Index}/{id?}"
    );

//app.MapRazorPages();

app.Run();
