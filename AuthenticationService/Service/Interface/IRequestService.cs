namespace AuthenticationService.Service.Interface;

public interface IRequestService
{
    Task<string> RequestRedirect(string guid, string scope);
}