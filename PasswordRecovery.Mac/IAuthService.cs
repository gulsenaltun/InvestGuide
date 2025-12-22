using System.ServiceModel;
using System.Threading.Tasks;

[ServiceContract]
public interface IAuthService
{
    [OperationContract]
    Task<string> CreatePasswordResetTokenAsync(string email);

    [OperationContract]
    Task<bool> ResetPasswordAsync(string token, string newPasswordHash);
}