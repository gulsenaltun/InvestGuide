using System.ServiceModel;

namespace PasswordRecovery.WCF
{
    [ServiceContract]
    public interface IAuthService
    {
        [OperationContract]
        bool CheckEmailExists(string email);

        [OperationContract]
        string CreatePasswordResetToken(string email);

        [OperationContract]
        bool ResetPassword(string token, string newPassword);
    }
}
