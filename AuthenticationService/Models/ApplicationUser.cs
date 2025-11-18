using Microsoft.AspNetCore.Identity;

namespace AuthenticationService.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<ClientAppDetail> ClientAppDetails { get; set; }
}