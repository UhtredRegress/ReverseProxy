namespace AuthenticationService.Models;

public class ClientAppType
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string RedirectUri { get; set; }
}