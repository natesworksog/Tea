using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tea
{
    static class StringExtensions
    {
        public static string[] SplitCommandLine(this string commandLine)
        {
            var matches = Regex.Matches(commandLine, @"(?<match>(?:""[^""]*""|[^ ])+)");
            var parts = new List<string>();

            foreach (Match match in matches)
            {
                parts.Add(match.Groups["match"].Value.Trim('"'));
            }

            return parts.ToArray();
        }
    }

    class Shell
    {
        static string currentDirectory = Directory.GetCurrentDirectory();
        static string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        static List<string> commandHistory = new List<string>();
        static int historyIndex = -1;

        static void Main(string[] args)
        {
            bool exit = false;

            while (!exit)
            {
                string autostartPath = Path.Combine(homeDirectory, ".config", "tea", "autostart.tea");
                if (File.Exists(autostartPath))
                {
                    ExecuteScript(autostartPath);
                }
                Console.Write($"{GetPrompt()}");
                string input = ReadCommandWithHistory();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                string[] commandArgs = input.SplitCommandLine();
                string command = commandArgs[0].ToLower();

                switch (command)
                {
                    case "exit":
                        exit = true;
                        break;
                    case "cd":
                        ChangeDirectory(commandArgs);
                        break;
                    case "pwd":
                        Console.WriteLine(Environment.CurrentDirectory);
                        break;
                    default:
                        if (File.Exists(command) && command.EndsWith(".tea", StringComparison.OrdinalIgnoreCase))
                        {
                            ExecuteScript(command);
                        }
                        else
                        {
                            ExecuteCommand(commandArgs);
                        }
                        break;
                }

                commandHistory.Insert(0, input);
                historyIndex = -1;
            }
        }

        static void ChangeDirectory(string[] commandArgs)
        {
            if (commandArgs.Length < 2)
            {
                Console.WriteLine("Usage: cd <directory>");
                return;
            }

            string newDirectory = commandArgs[1];

            if (newDirectory == "~")
            {
                newDirectory = homeDirectory;
            }
            else if (newDirectory.StartsWith("~" + Path.DirectorySeparatorChar))
            {
                newDirectory = Path.Combine(homeDirectory, newDirectory.Substring(2));
            }

            try
            {
                Directory.SetCurrentDirectory(newDirectory);
                currentDirectory = Directory.GetCurrentDirectory();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error changing directory: {ex.Message}");
                Console.ResetColor();
            }
        }


        static void ExecuteCommand(string[] commandArgs)
        {
            try
            {
                string command = commandArgs[0];
                string arguments = string.Join(" ", commandArgs.Skip(1));
                
                if (command.StartsWith("~"))
                {
                    string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    command = Path.Combine(homeDirectory, command.Substring(1));
                }
                
                if (File.Exists(command) && !command.Contains(Path.DirectorySeparatorChar))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process process = new Process { StartInfo = psi };
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Console.WriteLine(output);
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {error}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process process = new Process { StartInfo = psi };
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Console.WriteLine(output);
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {error}");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error executing command: {ex.Message}");
                Console.ResetColor();
            }
        }

        static string GetPrompt()
        {
            string user = Environment.UserName;
            string hostname = Environment.MachineName;
            string directory = currentDirectory.StartsWith(homeDirectory)
                ? $"~{currentDirectory.Substring(homeDirectory.Length)}"
                : currentDirectory;

            string prompt = $"\u001b[36m{user}\u001b[0m@\u001b[36m{hostname}\u001b[0m:\u001b[34m{directory}\u001b[0m $ ";
            return prompt;
        }
        
        static string ReadCommandWithHistory()
        {
            ConsoleKeyInfo keyInfo;
            var inputBuffer = new List<char>();
            int initialHistoryIndex = historyIndex;

            do
            {
                keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Backspace && inputBuffer.Count > 0)
                {
                    Console.Write("\b \b");
                    inputBuffer.RemoveAt(inputBuffer.Count - 1);
                }
                else if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    if (historyIndex < commandHistory.Count - 1)
                    {
                        historyIndex++;
                        ClearInput(inputBuffer);
                        Console.Write(commandHistory[historyIndex]);
                        inputBuffer.AddRange(commandHistory[historyIndex]);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    if (historyIndex >= 0)
                    {
                        historyIndex--;
                        ClearInput(inputBuffer);
                        if (historyIndex >= 0)
                        {
                            Console.Write(commandHistory[historyIndex]);
                            inputBuffer.AddRange(commandHistory[historyIndex]);
                        }
                    }
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write(keyInfo.KeyChar);
                    inputBuffer.Add(keyInfo.KeyChar);
                }
            } while (keyInfo.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return new string(inputBuffer.ToArray());
        }

        static void ClearInput(List<char> inputBuffer)
        {
            foreach (var _ in inputBuffer)
            {
                Console.Write("\b \b");
            }
            inputBuffer.Clear();
        }
        
        static void ExecuteScript(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        // Handle "~" at the beginning of the script file path
                        if (trimmedLine.StartsWith("~"))
                        {
                            string expandedPath = Path.Combine(homeDirectory, trimmedLine.Substring(1));
                            string[] commandArgs = expandedPath.SplitCommandLine();
                            ExecuteCommand(commandArgs);
                        }
                        else
                        {
                            string[] commandArgs = trimmedLine.SplitCommandLine();
                            ExecuteCommand(commandArgs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error executing commands from file: {ex.Message}");
                Console.ResetColor();
            }
        } 
    }
}