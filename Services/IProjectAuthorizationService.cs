namespace SonicPoints.Services
{
    public interface IProjectAuthorizationService
    {
        Task<bool> HasProjectRoleAsync(string userId, int projectId, params string[] roles);
    }

}
