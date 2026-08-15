using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FunctionCounter.Models;
using Microsoft.AspNetCore.Mvc;

namespace FunctionCounter.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new List<FunctionInfo>());
        }

        [HttpPost]
        public IActionResult Index(string projectPath, string language = "All")
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                ViewBag.ErrorMessage = "Please enter the project path.";
                return View(new List<FunctionInfo>());
            }
            if (!Directory.Exists(projectPath))
            {
                ViewBag.ErrorMessage = "The specified project path does not exist.";
                return View(new List<FunctionInfo>());
            }
            //Validate the projectPath and language inputs
            ViewBag.ProjectPath = projectPath;
            ViewBag.Language = language;
            List<FunctionInfo> functions =  new List<FunctionInfo>();

            //14aug2023 Shubham Added support for Java, C++, and Python function detection
            Dictionary<string, string> patterns = new Dictionary<string, string>
         {
            {
                ".cs",
                @"(?:public|private|protected|internal)\s+(?:(?:static|virtual|override|sealed|new|async)\s+)*[\w<>\[\],\.]+\s+(\w+)\s*\("
            },

            {
                ".java",
                @"(?:public|private|protected)\s+(?:static\s+)?[\w<>\[\]]+\s+(\w+)\s*\("
            },

            {
                ".cpp",
                @"(?:[\w:<>]+\s+)+(\w+)\s*\([^)]*\)\s*\{"
            },

            {
                ".py",
                @"def\s+(\w+)\s*\("
            }
         };

            List<string> files = new List<string>();
            
            if (language == "All")
            {
                foreach (string extension in patterns.Keys)
                {
                    files.AddRange(
                        Directory.GetFiles(
                            projectPath,
                            "*" + extension,
                            SearchOption.AllDirectories));
                }
            }
            else
            {
                files.AddRange(
                    Directory.GetFiles(
                        projectPath,
                        "*." + language,
                        SearchOption.AllDirectories));
            }

            //14Aug2026 Shubham Process each file and extract function names based on the language-specific regex patterns
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file);

                if (!patterns.ContainsKey(extension))
                {
                    continue;
                }

                Regex regex = new Regex( patterns[extension], RegexOptions.Multiline);

                string code = System.IO.File.ReadAllText(file);

                MatchCollection matches = regex.Matches(code);

                foreach (Match match in matches)
                {
                    functions.Add(new FunctionInfo
                    {
                        FileName = Path.GetFileName(file),
                        Language = extension.Replace(".", "").ToUpper(),
                        FunctionName = match.Groups[1].Value
                    });
                }
            }
            return View(functions);
        }

        //14Aug2023 Shubham Added a new action method to download the function names as a text file
        [HttpPost]
        public IActionResult Download(string projectPath, string language = "All")
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                TempData["ErrorMessage"] = "Please enter the project path.";

                return RedirectToAction("Index", new { projectPath, language });
            }

            if (!Directory.Exists(projectPath))
            {
                TempData["ErrorMessage"] = "The specified project path does not exist.";

                return RedirectToAction("Index", new { projectPath, language });
            }
            StringBuilder sb = new StringBuilder();

            //14aug2023 Shubham Added support for Java, C++, and Python function detection
            Dictionary<string, string> patterns = new Dictionary<string, string>
    {
        {
            ".cs",
            @"(?:public|private|protected|internal)\s+(?:(?:static|virtual|override|sealed|new|async)\s+)*[\w<>\[\],\.]+\s+(\w+)\s*\("
        },
        {
            ".java",
            @"(?:public|private|protected)\s+(?:static\s+)?[\w<>\[\]]+\s+(\w+)\s*\("
        },
        {
            ".cpp",
            @"(?:[\w:<>]+\s+)+(\w+)\s*\([^)]*\)\s*\{"
        },
        {
            ".py",
            @"def\s+(\w+)\s*\("
        }
    };

            List<string> files = new List<string>();

            if (language == "All")
            {
                foreach (string extension in patterns.Keys)
                {
                    files.AddRange(
                        Directory.GetFiles(
                            projectPath,
                            "*" + extension,
                            SearchOption.AllDirectories));
                }
            }
            else
            {
                files.AddRange(
                    Directory.GetFiles(
                        projectPath,
                        "*." + language,
                        SearchOption.AllDirectories));
            }
            //14Aug2026 Shubham Process each file and extract function names based on the language-specific regex patterns
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file);

                if (!patterns.ContainsKey(extension))
                {
                    continue;
                }

                Regex regex = new Regex(
                    patterns[extension],
                    RegexOptions.Multiline);

                string code = System.IO.File.ReadAllText(file);

                MatchCollection matches = regex.Matches(code);

                if (matches.Count > 0)
                {
                    sb.AppendLine("File: " + Path.GetFileName(file));
                    sb.AppendLine();

                    foreach (Match match in matches)
                    {
                        sb.AppendLine(match.Groups[1].Value);
                    }

                    sb.AppendLine();
                    sb.AppendLine(new string('-', 50));
                    sb.AppendLine();
                }
            }
            // Convert the StringBuilder content to a byte array for file download
            byte[] fileBytes = Encoding.UTF8.GetBytes(sb.ToString());

            return File(
                fileBytes,
                "text/plain",
                "FunctionsList.txt");
        }
    }
}
