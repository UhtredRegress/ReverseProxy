using AuthorizeServer.DTO;

namespace AuthorizeServer.Service.Interface;

public interface IOauthService
{
   Task<RequestAuthDto> ValidateRequestAsync(string key, string redirectUri);
}