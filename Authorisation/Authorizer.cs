using System.Linq;
using FFXIVVenues.Veni.Authorisation.Configuration;
using FFXIVVenues.VenueModels;

namespace FFXIVVenues.Veni.Authorisation;

public class Authorizer : IAuthorizer
{
    
    public const string VENUE_SOURCE_KEY = "_VenueSource";
    public const string MASTER_SOURCE_KEY = "_Master";

    private readonly AuthorisationConfiguration _configuration;
    
    public Authorizer(AuthorisationConfiguration configuration) =>
        this._configuration = configuration;

    public AuthorizationResult Authorize(ulong user, Permission permission, Venue venue = null)
    {
        if (_configuration.Master.Contains(user))
            return new(true, MASTER_SOURCE_KEY, permission, user, venue);

        var localPermission = venue is not null ? permission.ToLocalPermission(venue) : null;
        if (venue != null && venue.Managers.Contains(user.ToString()))
        {
            if (_configuration.ManagerPermissions.Contains(permission))
                return new(true, VENUE_SOURCE_KEY, permission, user, venue);
            if (localPermission.HasValue && _configuration.ManagerPermissions.Contains(localPermission.Value))
                return new(true, VENUE_SOURCE_KEY, localPermission.Value, user, venue);
        }
        foreach (var set in _configuration.PermissionSets)
        {
            if (!set.Members.Contains(user)) continue;
            if (set.Permissions.Contains(permission))
                return new(true, set.Name, permission, user, venue);
            if (localPermission.HasValue && set.Permissions.Contains(localPermission.Value))
                return new(true, set.Name, localPermission.Value, user, venue);
        }

        return new(false, null, permission, user, venue);
    }
    
}