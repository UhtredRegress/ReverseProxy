using AuthenticationService.Controller;
using AuthenticationService.Models;

namespace AuthenticationService.Service.Interface;

public interface IQueryService
{
    Task<bool> CheckPaid(string scope, string guid);
}