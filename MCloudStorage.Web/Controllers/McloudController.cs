
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text;

namespace MCloudStorage.Web.Controllers
{
    [Route("identity")]
    [Authorize]
    public class McloudController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<McloudController> _logger;

        public McloudController(IHttpClientFactory httpClientFactory,
            ILogger<McloudController> logger)
        {
            _httpClientFactory = httpClientFactory ??
                throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //[Authorize]
        public async Task<IActionResult> Index()
        {
            await LogIdentityInformation();

            return View();
        }

        public async Task LogIdentityInformation()
        {
            // get the saved identity token
            var identityToken = await HttpContext
                .GetTokenAsync(OpenIdConnectParameterNames.IdToken);

            var userClaimsStringBuilder = new StringBuilder();
            foreach (var claim in User.Claims)
            {
                userClaimsStringBuilder.AppendLine(
                    $"Claim type: {claim.Type} - Claim value: {claim.Value}");
            }

            // log token & claims
            _logger.LogInformation($"Identity token & user claims: " +
                $"\n{identityToken} \n{userClaimsStringBuilder}");
        }
    }
}
