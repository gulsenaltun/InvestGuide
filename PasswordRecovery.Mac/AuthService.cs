public class AuthService : IAuthService
{
    public async Task<string> CreatePasswordResetTokenAsync(string email)
    {
        // Burada normalde DB'ye bakıp token üretilir
        // Şimdilik test için rastgele bir string dönelim
        return await Task.FromResult("token_" + Guid.NewGuid().ToString().Substring(0,8));
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPasswordHash)
    {
        // Burada DB'de şifre güncellenir
        return await Task.FromResult(true);
    }
}