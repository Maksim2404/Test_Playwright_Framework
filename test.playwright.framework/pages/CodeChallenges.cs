using System.Text.RegularExpressions;
using Microsoft.Playwright;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.pages;

public class CodeChallenges(IPage page) : BaseProjectElements(page)
{
    public class LoginProcessor
    {
        private static class LoginService
        {
            public static bool Authenticate(string username, string password)
            {
                return password == "secure123";
            }
        }

        private static void ProcessLogins(List<string> logins)
        {
            foreach (var entry in logins)
            {
                var fields = entry.Split(':');

                if (fields.Length < 2 || string.IsNullOrEmpty(fields[1]))
                {
                    continue;
                }

                var isAuthenticated = LoginService.Authenticate(fields[0], fields[1]);

                Console.WriteLine(isAuthenticated
                    ? $"Login successful for user: {fields[0]}"
                    : $"Login failed for user: {fields[0]}");
            }
        }

        public static void GetLoginProcessor()
        {
            var logins = new List<string>
            {
                "user1:secure123",
                "user2:wrongpass",
                "admin:secure123",
                "guest:",
                "invalid"
            };

            ProcessLogins(logins);
        }
    }

    public class DuplicateLoginFinder
    {
        public static void FindDuplicates(List<string> logins)
        {
            var seen = new HashSet<string>();
            var duplicates = new HashSet<string>();

            foreach (var fields in from entry in logins
                     select entry.Split(':')
                     into fields
                     where fields.Length >= 2 && !string.IsNullOrEmpty(fields[1])
                     where !seen.Add(fields[0])
                     select fields)
            {
                duplicates.Add(fields[0]);
            }

            Console.WriteLine("Duplicate usernames: " + string.Join(", ", duplicates));
        }
    }

    public class PasswordValidator
    {
        private static bool IsValidPassword(string password)
        {
            return Regex.IsMatch(password, @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{8,}$");
        }

        public static void ValidatePassword()
        {
            Console.WriteLine(IsValidPassword("StrongP@ss1"));
            Console.WriteLine(IsValidPassword("weakpass"));
        }
    }
}