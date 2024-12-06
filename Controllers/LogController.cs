using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;

namespace YourNamespace.Controllers
{
    public class LogController : Controller
    {
        private readonly string _connectionString = "Data Source=ExpenseTracker.db";

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Success(string username, string password)
        {
            // Check if the user is the admin
            if (username == "budgetadmin" && password == "thebudgetbuddyadmin")
            {
                // Redirect to the admin dashboard
                return RedirectToAction("Index", "Admin");
            }

            // Validate if the user exists and credentials are correct
            if (ValidateUser(username, password))
            {
                // Fetch the UserId
                int userId = GetUserId(username, password);

                if (userId > 0)
                {
                    // Update the streak
                    UpdateStreak(userId);

                    // Store UserId and Username in session
                    HttpContext.Session.SetInt32("UserId", userId);
                    HttpContext.Session.SetString("Username", username);

                    // Redirect to the user dashboard
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    // Handle unexpected issues (e.g., missing UserId in database)
                    ModelState.AddModelError("", "An error occurred. Please try again.");
                    return View("Index");
                }
            }
            else
            {
                // If credentials are invalid, return to the login page with an error
                ModelState.AddModelError("", "Invalid username or password.");
                return View("Index");
            }
        }

        private void UpdateStreak(int userId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // Fetch current streak and last login date
                command.CommandText = @"
                    SELECT StreakCount, LastLoginDate
                    FROM Users
                    WHERE UserId = @userId";
                command.Parameters.AddWithValue("@userId", userId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int streakCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        DateTime? lastLoginDate = reader.IsDBNull(1) ? null : reader.GetDateTime(1);

                        DateTime today = DateTime.UtcNow.Date;
                        if (lastLoginDate.HasValue)
                        {
                            if (lastLoginDate.Value.Date == today.AddDays(-1))
                            {
                                streakCount++; // Continue streak
                            }
                            else if (lastLoginDate.Value.Date != today)
                            {
                                streakCount = 1; // Reset streak
                            }
                        }
                        else
                        {
                            streakCount = 1; // First login
                        }

                        // Update streak and last login date
                        var updateCommand = connection.CreateCommand();
                        updateCommand.CommandText = @"
                            UPDATE Users
                            SET StreakCount = @streakCount,
                                LastLoginDate = @lastLoginDate
                            WHERE UserId = @userId";
                        updateCommand.Parameters.AddWithValue("@streakCount", streakCount);
                        updateCommand.Parameters.AddWithValue("@lastLoginDate", today);
                        updateCommand.Parameters.AddWithValue("@userId", userId);

                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private int GetUserId(string username, string password)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT UserId
                    FROM Users
                    WHERE Username = @username AND Password = @password";
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);

                var result = command.ExecuteScalar();
                return result == null ? -1 : Convert.ToInt32(result);
            }
        }

        [HttpPost]
        public IActionResult Register(string username, string email, string password)
        {
            if (UserExists(username))
            {
                ModelState.AddModelError("", "Username already exists. Please choose a different username.");
                return View("Index");
            }

            if (RegisterUser(username, email, password))
            {
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View("Index");
            }
        }

        private bool ValidateUser(string username, string password)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(1) 
                    FROM Users 
                    WHERE Username = @username AND Password = @password";
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private bool UserExists(string username)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(1) 
                    FROM Users 
                    WHERE Username = @username";
                command.Parameters.AddWithValue("@username", username);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private bool RegisterUser(string username, string email, string password)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Users (Username, Email, Password) 
                    VALUES (@username, @email, @password)";
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@password", password);

                return command.ExecuteNonQuery() > 0;
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Clear the session to log out the user
            HttpContext.Session.Clear();

            // Redirect to the login page
            return RedirectToAction("Index");
        }
    }
}
