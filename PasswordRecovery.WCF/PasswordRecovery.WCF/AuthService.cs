using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace PasswordRecovery.WCF
{
    public class AuthService : IAuthService
    {
        // Web.config dosyasındaki bağlantı dizesini okur
        private readonly string connStr = ConfigurationManager.ConnectionStrings["MySqlConn"].ConnectionString;

        // 1️⃣ Email var mı kontrolü
        public bool CheckEmailExists(string email)
        {
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM Users WHERE email=@email";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@email", email);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        // 2️⃣ Reset Token üretme
        public string CreatePasswordResetToken(string email)
        {
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                // Kullanıcı ID'sini al
                string userSql = "SELECT user_id FROM Users WHERE email=@email";
                var userCmd = new MySqlCommand(userSql, conn);
                userCmd.Parameters.AddWithValue("@email", email);

                var userIdObj = userCmd.ExecuteScalar();
                if (userIdObj == null)
                    return null;

                int userId = Convert.ToInt32(userIdObj);

                // Token ve Süre oluştur
                string token = Guid.NewGuid().ToString();
                DateTime expire = DateTime.Now.AddMinutes(15);

                // Token'ı veritabanına kaydet
                string insertSql = @"INSERT INTO PasswordResetTokens
                                     (user_id, token, expire_date)
                                     VALUES (@uid, @token, @expire)";

                var insertCmd = new MySqlCommand(insertSql, conn);
                insertCmd.Parameters.AddWithValue("@uid", userId);
                insertCmd.Parameters.AddWithValue("@token", token);
                insertCmd.Parameters.AddWithValue("@expire", expire);
                insertCmd.ExecuteNonQuery();

                return token;
            }
        }

        // 3️⃣ Şifre sıfırlama (DÜZELTİLEN KISIM)
        public bool ResetPassword(string token, string newPassword)
        {
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                // Token geçerli mi kontrol et
                string checkSql = @"SELECT user_id FROM PasswordResetTokens
                                    WHERE token=@token
                                    AND expire_date > NOW()
                                    AND is_used = 0";

                var checkCmd = new MySqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@token", token);

                var userIdObj = checkCmd.ExecuteScalar();
                if (userIdObj == null)
                    return false; // Token geçersiz veya süresi dolmuş

                int userId = Convert.ToInt32(userIdObj);

                // --- GÜNCELLEME: Hashleme burada YAPILMIYOR ---
                // Web tarafından zaten hashlenmiş geldiği için direkt kaydediyoruz.

                string updateUser = "UPDATE Users SET password_hash=@pwd WHERE user_id=@uid";
                var updateCmd = new MySqlCommand(updateUser, conn);
                updateCmd.Parameters.AddWithValue("@pwd", newPassword);
                updateCmd.Parameters.AddWithValue("@uid", userId);
                updateCmd.ExecuteNonQuery();

                // Token'ı kullanıldı olarak işaretle
                string markUsed = "UPDATE PasswordResetTokens SET is_used=1 WHERE token=@token";
                var usedCmd = new MySqlCommand(markUsed, conn);
                usedCmd.Parameters.AddWithValue("@token", token);
                usedCmd.ExecuteNonQuery();

                return true;
            }
        }
    }
}