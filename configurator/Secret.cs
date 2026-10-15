using System;
using System.Security.Cryptography;
using System.Text;

namespace CrewRestConfigurator
{
    /// <summary>
    /// Verschlüsselt das Mail-App-Passwort mit der Windows-DPAPI (Benutzerkontext).
    /// Der verschlüsselte Wert steht mit dem Präfix "enc:" in der config.txt und
    /// laesst sich nur von demselben Windows-Benutzer auf demselben PC wieder lesen.
    /// </summary>
    internal static class Secret
    {
        public const string Prefix = "enc:";

        public static bool IsProtected(string value)
        {
            return !string.IsNullOrEmpty(value)
                && value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }
            if (IsProtected(plainText))
            {
                return plainText;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                return Prefix + Convert.ToBase64String(encrypted);
            }
            catch (Exception)
            {
                // Lieber Klartext als gar keine Funktion.
                return plainText;
            }
        }

        public static string Unprotect(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            if (!IsProtected(value))
            {
                return value;
            }

            try
            {
                byte[] encrypted = Convert.FromBase64String(value.Substring(Prefix.Length));
                byte[] data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
