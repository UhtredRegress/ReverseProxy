using AuthenticationService.Controller;
using AuthenticationService.Models;
using AuthenticationService.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Service;

public class QueryService : IQueryService
{
    private readonly ApplicationDbContext _db;

    public QueryService(ApplicationDbContext db)
    {
        _db = db;
    }
    public async Task<bool> CheckPaid(string scope, string guid)
    {
        var foundService = await _db.ClientAppDetails.Where(dt => dt.Type.Type == scope && dt.User.Id == guid).AsNoTracking().FirstOrDefaultAsync();
        return foundService == null ? false : foundService.Purchased;
    }
}