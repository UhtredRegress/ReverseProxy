namespace AuthenticationService.Models;

public class ClientAppDetail
{
    public int Id { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public ApplicationUser User { get; set; }
    public bool Purchased { get; set; }

    public ClientAppType Type { get; set; }
}