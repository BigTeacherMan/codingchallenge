using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace coding_challenge
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Establishing the base url that will be combined with the BuildQueryURL method
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://randomuser.me/");

            // Calling of methods to determine how many users will be pulled from API, with which seed, and which parameters
            int results = GetNumUsers();
            string seed = GetSeed();
            string[] parameters = GetParameters();

            // Try and catch to ensure the HTTP request succeeds after using the built URL
            try
            {
                string url = BuildQueryURL(results, seed, parameters);
                HttpResponseMessage response = await client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                // Logging the response body for debugging
                Console.WriteLine("API Response: ");
                Console.WriteLine(responseBody);

                var root = JsonSerializer.Deserialize<Root>(responseBody);

                if (root == null || root.results == null || root.results.Count == 0)
                {
                    Console.WriteLine("Failed to fetch or deserialize users.");
                    return;
                }

                if (!ValidateNationalities(root.results))
                {
                    Console.WriteLine("The number of nationalities is not four or one of them has a 40% or higher occurrence rate.");
                    return;
                }

                var userDataList = ConvertToCSV(root.results);

                WriteToCSV(userDataList, "Dunford_Curtis_users.csv");

                Console.WriteLine("CSV file created successfully.");

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
            catch (JsonException e)
            {
                Console.WriteLine($"JSON deserialization error: {e.Message}");
            }
        }

        static string GetSeed()
        {
            string seed;
            Console.WriteLine("Which seed do you want to use?");
            while (string.IsNullOrWhiteSpace(seed = Console.ReadLine()))
            {
                Console.WriteLine("Please enter a seed. The seed cannot be empty.");
            }
            Console.WriteLine("You have chosen the " + seed + " seed.");
            return seed;
        }

        static int GetNumUsers()
        {
            int results;
            Console.WriteLine("How many users would you like to collect?");
            while (!int.TryParse(Console.ReadLine(), out results) || results <= 0)
            {
                Console.WriteLine("Please enter a valid integer number.");
            }
            Console.WriteLine("You have chosen " + results + " users.");
            return results;
        }

        static string[] GetParameters()
        {
            List<string> parametersList = new List<string>();
            Console.WriteLine("Would you like to select the parameters that are taken from the API? (yes/no)");
            string response = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(response) || (response.ToLower() != "yes" && response.ToLower() != "no"))
            {
                Console.WriteLine("Please provide a yes or no answer.");
                response = Console.ReadLine();
            }

            if (response.ToLower() == "yes")
            {
                do
                {
                    Console.WriteLine("Which parameter would you like to add?");
                    string parameter = Console.ReadLine();

                    while (string.IsNullOrWhiteSpace(parameter))
                    {
                        Console.WriteLine("Parameter cannot be empty. Please enter a valid parameter.");
                        parameter = Console.ReadLine();
                    }

                    parametersList.Add(parameter);
                    Console.WriteLine("Would you like to add another parameter? (yes/no)");
                    response = Console.ReadLine();

                    while (string.IsNullOrWhiteSpace(response) || (response.ToLower() != "yes" && response.ToLower() != "no"))
                    {
                        Console.WriteLine("Please provide a yes or no answer.");
                        response = Console.ReadLine();
                    }

                } while (response.ToLower() == "yes");

                Console.WriteLine("You have chosen the following parameters:");
                foreach (string param in parametersList)
                {
                    Console.WriteLine($"{param}");
                }
            }
            else
            {
                Console.WriteLine("No parameters selected.");
            }

            return parametersList.ToArray();
        }

        static string BuildQueryURL(int results, string seed, string[] parameters)
        {
            string URLParameters = string.Join(",", parameters);
            return $"api/?results={results}&seed={seed}&inc={URLParameters}";
        }

        static int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }

        static int GetGenderCode(string gender)
        {
            return gender.ToLower() switch
            {
                "male" => 0,
                "female" => 1,
                _ => 2,
            };
        }

        static bool ValidateNationalities(List<Result> results)
        {
            Dictionary<string, int> nationalityCounts = new Dictionary<string, int>();

            foreach (var result in results)
            {
                if (result.nat != null)
                {
                    string nationality = result.nat.ToLower();
                    if (nationalityCounts.ContainsKey(nationality))
                    {
                        nationalityCounts[nationality]++;
                    }
                    else
                    {
                        nationalityCounts[nationality] = 1;
                    }
                }
            }

            int totalUsers = results.Count;
            foreach (var count in nationalityCounts.Values)
            {
                if ((double)count / totalUsers > 0.4)
                {
                    return false;
                }
            }

            return nationalityCounts.Count >= 4;
        }

        static List<UserDataCSV> ConvertToCSV(List<Result> results)
        {
            var random = new Random();
            return results.Select(result => new UserDataCSV
            {
                ID = random.Next(1000000, 100000000),
                FirstName = result.name.first,
                LastName = result.name.last,
                Gender = GetGenderCode(result.gender),
                Email = result.email,
                Username = result.login.username,
                DOB = result.dob.date.ToString("yyyy/MM/dd"),
                Age = CalculateAge(result.dob.date)
            }).ToList();
        }

        static void WriteToCSV(List<UserDataCSV> userDataList, string fileName)
        {
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string fullPath = Path.Combine(downloadsPath, fileName);

            using (var writer = new StreamWriter(fullPath))
            {
                writer.WriteLine("ID,FirstName,LastName,Gender,Email,Username,DOB,Age");

                foreach (var user in userDataList)
                {
                    writer.WriteLine($"{user.ID},{user.FirstName},{user.LastName},{user.Gender},{user.Email},{user.Username},{user.DOB},{user.Age}");
                }
            }
        }
    }


    public class Root
    {
        public List<Result> results { get; set; }
    }

    public class Result
    {
        public string gender { get; set; }
        public Name name { get; set; }
        public string email { get; set; }
        public Login login { get; set; }
        public Dob dob { get; set; }
        public string nat { get; set; }
    }

    public class Name
    {
        public string first { get; set; }
        public string last { get; set; }
    }

    public class Login
    {
        public string username { get; set; }
    }

    public class Dob
    {
        public DateTime date { get; set; }
    }


    public class UserDataCSV
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Gender { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string DOB { get; set; }
        public int Age { get; set; }
    }
}
