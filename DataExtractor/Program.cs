using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text.Json;

public class JsonPatient
{
    public string Name { get; set; }
    public int NHSNumber { get; set; }
}

public class Patient
{
    public string Name { get; set; }
    public string NHSNumber { get; set; }
}

public class Program
{
    private static string CleanNHSNumber(string nhsNumber)
    {
        // Remove all non-numeric characters
        return new string(nhsNumber.Where(char.IsDigit).ToArray());
    }

    public static List<Patient> ProcessFirstFormat(string input)
    {
        var patients = new List<Patient>();
        
        // Replace all variations of [[new-line]] with actual new line
        var normalizedText = Regex.Replace(input, @"\[\[(?i)new-line\]\]", "\n");

        // Extract Name: followed by NHS Number: pairs
        var nameNhsPattern = @"Name:\s*(\w+(?:\s+\w+)?)\s+NHS\s*(?:Number|NUmber):\s*([^\s\n]+)";
        var nameNhsMatches = Regex.Matches(normalizedText, nameNhsPattern);
        foreach (Match match in nameNhsMatches)
        {
            patients.Add(new Patient
            {
                Name = match.Groups[1].Value.Trim(),
                NHSNumber = CleanNHSNumber(match.Groups[2].Value.Trim())
            });
        }

        // Process NHS Number: followed by name pattern
        var nhsWithNamePattern = @"NHS\s*Number:\s*(\d+)\s+(\w+(?:\s+\w+)?)";
        var nhsWithNameMatches = Regex.Matches(normalizedText, nhsWithNamePattern);
        foreach (Match match in nhsWithNameMatches)
        {
            patients.Add(new Patient
            {
                Name = match.Groups[2].Value.Trim(),
                NHSNumber = CleanNHSNumber(match.Groups[1].Value.Trim())
            });
        }

        // Process NHS Numbers that are alone on a line (these should be marked as Unknown)
        var nhsAlonePattern = @"NHS\s*Number:\s*(\d+)";
        var nhsAloneMatches = Regex.Matches(normalizedText, nhsAlonePattern);
        foreach (Match match in nhsAloneMatches)
        {
            patients.Add(new Patient
            {
                Name = "Unknown",
                NHSNumber = CleanNHSNumber(match.Groups[1].Value.Trim())
            });
        }
        
        return patients;
    }
    
    public static List<Patient> ProcessSecondFormat(string input)
    {
        try
        {
            // Clean up the JSON format
            input = input.Replace("{[", "[").Replace("]}", "]");
            
            // Parse JSON array
            var jsonPatients = JsonSerializer.Deserialize<List<JsonPatient>>(input);
            
            // Convert to Patient objects with string NHS numbers
            return jsonPatients.Select(jp => new Patient
            {
                Name = string.IsNullOrWhiteSpace(jp.Name) ? "Unknown" : jp.Name,
                NHSNumber = CleanNHSNumber(jp.NHSNumber.ToString())
            }).ToList();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing JSON format: {ex.Message}");
            return new List<Patient>();
        }
    }
    
    public static void Main()
    {
        string firstString = @"Today we saw 3 patients[[new-line]]The first was Name:Michael Michaelson NHS Number:333444[[New-Line]]the second was Name:Jane Bridge NHS NUmber:55666 with her son Name:David Bridge NHS Number:a44t55[[new-line]]We then saw[[new-line]]NHS Number:999 James McDonald[[new-line]][[new-line]]NHS Number:444";
        string secondString = @"{[{""Name"":""James Jamerson"",""NHSNumber"":12345},{""Name"":""Bob Sinclair"",""NHSNumber"":5555},{""Name"":""Sally Jamerson"",""NHSNumber"":66554},{""Name"":""Michael Myers"",""NHSNumber"":6666},{""Name"":""James Jamerson"",""NHSNumber"":12345}]}";

        // Process both formats
        var patients1 = ProcessFirstFormat(firstString);
        var patients2 = ProcessSecondFormat(secondString);

        // Combine both lists and remove duplicates
        var allPatients = patients1.Concat(patients2)
            .GroupBy(p => p.NHSNumber)
            .Select(g => g.First())
            .OrderBy(p => p.Name)
            .ToList();

        // Print table header
        Console.WriteLine("|{0}|{1}|", new string('-', 20), new string('-', 14));
        Console.WriteLine("| {0,-18} | {1,-12} |", "Name", "NHS Number");
        Console.WriteLine("|{0}|{1}|", new string('-', 20), new string('-', 14));

        // Print each patient
        foreach (var patient in allPatients)
        {
            Console.WriteLine("| {0,-18} | {1,-12} |", patient.Name, patient.NHSNumber);
        }
        Console.WriteLine("|{0}|{1}|", new string('-', 20), new string('-', 14));
    }
}