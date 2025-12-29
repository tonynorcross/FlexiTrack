using Microsoft.AspNetCore.Authorization;

namespace FlexiTrack.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string SystemAdmin = "SystemAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string AnyAdmin = "AnyAdmin";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(SystemAdmin, policy =>
            policy.RequireClaim("isSystemAdmin", "true"));

        options.AddPolicy(CompanyAdmin, policy =>
            policy.RequireClaim("isCompanyAdmin", "true"));

        options.AddPolicy(AnyAdmin, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "isSystemAdmin" && c.Value == "true") ||
                context.User.HasClaim(c => c.Type == "isCompanyAdmin" && c.Value == "true")));
    }
}
