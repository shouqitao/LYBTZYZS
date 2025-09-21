namespace LYBT.Module.Auth.Interfaces
{
    public interface ISysAdminHandler
    {
        bool IsSysAdmin(string username);
        Task<string?> GetSysAdminPasswordHashAsync();
    }
}