using System;
using System.Net;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Diagnostics;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System.Collections.Generic;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Microsoft.Extensions.Options;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using FireSharp.Interfaces;
using System.Security.Cryptography;

class Program
{


public class Teacher
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public string Faculty { get; set; }
    public string ImageURL { get; set; }
    public string OriginalPageURL { get; set; }
    public string ScientificInterests { get; set; }
    public List<List<Mark>> MarksArrays { get; set; }
    public List<Comment> ArrayOfComments { get; set; }
    public List<Discipline> ArrayOfDiscipline { get; set; }
}

public class Mark
{
    public string MarkType { get; set; }
    public string AddresseeID { get; set; }
    public int MarkValue { get; set; }
    public string DataOfPublishing { get; set; }
    public string PublisherID { get; set; }
}

public class Comment
{
    public string AddresseeID { get; set; }
    public string DataOfPublishing { get; set; }
    public string PublisherID { get; set; }
    public string TextOfComment { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsPositive { get; set; } = true;
}

public class Discipline
{
    public string Id { get; set; }
    public string NameOfDiscipline { get; set; }
    public string DisciplinePageURL { get; set; }
    public string DescriptionOfDiscipline { get; set; }
    public string[] ArrayOfTeacher { get; set; }
    public List<List<Mark>> MarksArrays { get; set; }
    public List<Comment> ArrayOfComments { get; set; }
}


public class FacultyLink
    {
        public string FacultyName { get; set; }
        public string Url { get; set; }
    }

    static async Task Main()
    {
       

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.OutputEncoding = Encoding.UTF8;
       
        //string[] LNUTeachers =
        //{
        //    "https://physics.lnu.edu.ua/about/staff",
        //    "https://bioweb.lnu.edu.ua/about/staff",
        //    "https://geography.lnu.edu.ua/about/staff",
        //    "https://geology.lnu.edu.ua/about/staff",
        //    "https://econom.lnu.edu.ua/about/staff",
        //    //"https://electronics.lnu.edu.ua/about/staff/",
        //    //"https://journ.lnu.edu.ua/about/staff",
        //    //"https://lingua.lnu.edu.ua/about/staff",
        //    //"https://clio.lnu.edu.ua/about/staff",
        //    //"https://kultart.lnu.edu.ua/about/staff",
        //    //"https://new.mmf.lnu.edu.ua/about/staff",
        //    //"https://intrel.lnu.edu.ua/about/staff",
        //    //"https://pedagogy.lnu.edu.ua/about/staff",  //пед освіти , останній поки
        //};



        var LNUTeachers = new List<FacultyLink>
        {
            new FacultyLink { FacultyName = "Physics", Url = "https://physics.lnu.edu.ua/about/staff" },
            new FacultyLink { FacultyName = "Bioweb", Url = "https://bioweb.lnu.edu.ua/about/staff" },
            new FacultyLink { FacultyName = "Geography", Url = "https://geography.lnu.edu.ua/about/staff" },
            new FacultyLink { FacultyName = "Geology", Url = "https://geology.lnu.edu.ua/about/staff" },
            new FacultyLink { FacultyName = "Econom", Url = "https://econom.lnu.edu.ua/about/staff" },
        };



        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();


        var teachers = await ParsingTeachersAsync(LNUTeachers);

        //foreach (var teacher in teachers)
        //{
        //    Console.WriteLine($"{teacher.Value} - {teacher.Key}");
        //}
        Console.WriteLine($"Total: {GetTeachersCountFromFirebase().Result}");
        stopwatch.Stop();
        Console.WriteLine($"Total time taken: {stopwatch.ElapsedMilliseconds} ms");
    }
    public static async Task<int> GetTeachersCountFromFirebase()
    {
        try
        {
            var config = new FireSharp.Config.FirebaseConfig
            {
                AuthSecret = "fH1NntjCAbadkxnxDb6QmmXzlkVjX4WgSCzhG5KE",
                BasePath = "https://uniproject-b9c81-default-rtdb.europe-west1.firebasedatabase.app/"
            };

            using (var client = new FireSharp.FirebaseClient(config))
            {
                if (client != null)
                {
                    var response = await client.GetAsync("teachers");
                    if (response.Body != "null") // якщо не null, то вузол існує
                    {
                        var data = response.ResultAs<Dictionary<string, Dictionary<string, Teacher>>>();
                        int totalCount = 0;
                        foreach (var faculty in data)
                        {
                            totalCount += faculty.Value.Count;
                        }
                        return totalCount;
                    }
                    else
                    {
                        Console.WriteLine("No teachers found in the database");
                        return 0;
                    }
                }
                else
                {
                    Console.WriteLine("Firebase connection failed");
                    return -1; // якщо з'єднання не вдалося
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while getting teachers count from Firebase: " + ex.Message);
            return -1; // якщо сталася помилка
        }
    }


    public static async Task UploadTeacherToFirebase(Teacher teacher)
    {
        try
        {
            var config = new FireSharp.Config.FirebaseConfig
            {
                AuthSecret = "fH1NntjCAbadkxnxDb6QmmXzlkVjX4WgSCzhG5KE",
                BasePath = "https://uniproject-b9c81-default-rtdb.europe-west1.firebasedatabase.app/"
            };

            using (var client = new FireSharp.FirebaseClient(config))
            {
                if (client != null)
                {
                    var response = await client.GetAsync($"teachers/{teacher.Id}");
                    if (response.Body != "null") // якщо не null, то запис вже існує у базі даних
                    {
                        Console.WriteLine($"Teacher {teacher.Name} already exists in the database");
                    }
                    else
                    {
                        var setResponse = await client.SetAsync($"LNU/teachers/{teacher.Faculty}/{teacher.Id}", teacher); // Змінено шлях зберігання
                        if (setResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            Console.WriteLine($"Teacher {teacher.Name} uploaded successfully");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to upload teacher {teacher.Name}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Firebase connection failed");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while uploading teachers to Firebase: " + ex.Message);
        }
    }


    public static async Task<Dictionary<string, List<Teacher>>> ParsingTeachersAsync(List<FacultyLink> facultyLinks)
    {
        var teachersDictionary = new Dictionary<string, List<Teacher>>();

        foreach (var facultyLink in facultyLinks)
        {
            var facultyName = facultyLink.FacultyName;
            var url = facultyLink.Url;

            var teachers = new List<Teacher>();
            var parsedUrls = new HashSet<string>();

            using (var httpClient = new HttpClient())
            {
                try
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36");
                    var html = await httpClient.GetStringAsync(url).ConfigureAwait(false);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var teacherNodes = doc.DocumentNode.SelectNodes("//article[contains(@class, 'content')]//a[contains(@href, '/employee/')]");

                    if (teacherNodes != null)
                    {
                        foreach (var node in teacherNodes)
                        {
                            var profileUrl = node?.Attributes["href"].Value.Trim() ?? "";
                            if (parsedUrls.Contains(profileUrl))
                            {
                                continue;
                            }

                            var name = node?.InnerText.Trim() ?? "";
                            var positionNode = node.ParentNode.SelectSingleNode(".//following-sibling::td[@class='position']");
                            var position = positionNode?.InnerText.Trim() ?? "";
                            var emailNode = node.ParentNode.SelectSingleNode(".//following-sibling::td[@class='email']");
                            var email = emailNode?.InnerText.Trim() ?? "";

                            var profileHtml = await httpClient.GetStringAsync(profileUrl).ConfigureAwait(false);
                            var profileDoc = new HtmlDocument();
                            profileDoc.LoadHtml(profileHtml);

                            var imageUrlNode = profileDoc.DocumentNode.SelectSingleNode("//span[@class='photo']");
                            var styleAttribute = imageUrlNode?.Attributes["style"]?.Value?.Trim();
                            string imageUrl = null;
                            if (!string.IsNullOrEmpty(styleAttribute))
                            {
                                int startIndex = styleAttribute.IndexOf("url(");
                                int endIndex = styleAttribute.LastIndexOf(")");
                                if (startIndex != -1 && endIndex != -1)
                                {
                                    imageUrl = styleAttribute.Substring(startIndex + 4, endIndex - startIndex - 4);
                                }
                            }

                            var interestsSection = profileDoc.DocumentNode.SelectSingleNode("//section[h2='Наукові інтереси']");

                            var interests = interestsSection?.InnerText.Trim() ?? ""; 
                            
       
                            var disciplinesSection = profileDoc.DocumentNode.SelectSingleNode("//section[h2='Навчальні дисципліни']");

                            List<Discipline> disciplineList = new List<Discipline>();
                            if (disciplinesSection != null)
                            {
                                var disciplineNodes = disciplinesSection.SelectNodes(".//ul/li/a");
                                if (disciplineNodes != null)
                                {
                                    foreach (var d in disciplineNodes)
                                    {
                                        var disiplineObject = new Discipline();
                                        disiplineObject.NameOfDiscipline = d.InnerText.Trim();
                                        disiplineObject.DisciplinePageURL = d.Attributes["href"].Value;
                                        disciplineList.Add(disiplineObject);
                                    }
                                }
                            }

                            var teacher = new Teacher
                            {
                                Id = GetHash(name),
                                Name = name,
                                Position = position,
                                Email = email,
                                OriginalPageURL = profileUrl,
                                ImageURL = imageUrl,
                                Faculty = facultyName,
                                ScientificInterests = interests,
                                ArrayOfDiscipline = disciplineList,
                                MarksArrays = new List<List<Mark>>
                                {
                                    new List<Mark>
                                    {
                                        new Mark
                                        {
                                            MarkType = "Об'єктивність оцінювання",
                                            AddresseeID = "",
                                            MarkValue = 0,
                                            DataOfPublishing = "",
                                            PublisherID = ""
                                        }
                                    },
                                    new List<Mark>
                                    {
                                        new Mark
                                        {
                                            MarkType = "Методичне забезпечення",
                                            AddresseeID = "",
                                            MarkValue = 0,
                                            DataOfPublishing = "",
                                            PublisherID = ""
                                        }
                                    },
                                    new List<Mark>
                                    {
                                        new Mark
                                        {
                                            MarkType = "Якість викладання",
                                            AddresseeID = "",
                                            MarkValue = 0,
                                            DataOfPublishing = "",
                                            PublisherID = ""
                                        }
                                    }
                                },
                                ArrayOfComments = new List<Comment>(),
                            };

                            teachers.Add(teacher);

                            // Додати URL до словника парсованих URL
                            parsedUrls.Add(profileUrl);
                            await UploadTeacherToFirebase(teacher);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No teachers found for {url}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            // Додамо список викладачів для поточного факультету до словника
            teachersDictionary.Add(facultyName, teachers);
        }

        return teachersDictionary;
    }


    private static string GetHash(string input)
    {
        using (var algorithm = SHA256.Create())
        {
            var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }

}