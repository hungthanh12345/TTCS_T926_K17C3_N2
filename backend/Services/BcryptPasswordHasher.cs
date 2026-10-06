using System.Text;

namespace InternshipManagementApi.Services
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string passwordHash);
    }

    public class BcryptPasswordHasher : IPasswordHasher
    {
        private const int MaximumUtf8PasswordBytes = 72;

        public string Hash(string password)
        {
            if (Encoding.UTF8.GetByteCount(password) > MaximumUtf8PasswordBytes)
            {
                throw new ArgumentException(
                    $"Password cannot exceed {MaximumUtf8PasswordBytes} UTF-8 bytes.",
                    nameof(password));
            }

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        public bool Verify(string password, string passwordHash)
        {
            if (Encoding.UTF8.GetByteCount(password) > MaximumUtf8PasswordBytes)
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
