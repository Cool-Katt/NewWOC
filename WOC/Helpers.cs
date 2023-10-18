using System.Data;
using System.Data.SqlClient;
using OfficeOpenXml;
using Serilog;

namespace WOC;

public class Helpers
{
    private static string? _path;
    private static string? _queryStoreName;
    private static string _connectionString = null!;
    private static ExcelPackage _excelPackage = null!;

    public static void Init(IConfiguration settings)
    {
        //a method for initializing the static class so it can have access to the applicationSettings
        _path = settings["QueryStoreDefaultPath"] ?? AppDomain.CurrentDomain.BaseDirectory;
        _queryStoreName = settings["QueryStoreDefaultName"] ?? "QueryStore";
        _connectionString = settings["ConnectionStrings:Atoll"] ?? throw new NullReferenceException();
        _excelPackage = new ExcelPackage(new MemoryStream());
    }

    public static ExcelPackage GenerateExcelFile(string technology, string siteId)
    {
        //Send this command once before everything else.
        ExecuteQueryOnDB("EXEC DEV.[WOC].[UPDATE_WOC_tech_tables];");
        //Execute each selected file and write the result to _excelPackage
        foreach (var filename in GetFileList(MakeTag(technology)))
        {
            //see bellow
            try
            {
                WriteToExcel(filename, _excelPackage, siteId);
            }
            catch (Exception e)
            {
                var message = $"Could not execute query {filename}";
                Log.Error(e, message);
                throw new Exception($"Could not execute query {filename}\nCheck the logs for more details.");
            }
        }
        return _excelPackage;
    }

    private static DataTable ExecuteQueryOnDB(string query)
    {
        //self explanatory
        var dataTable = new DataTable();
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();
        dataTable.Load(reader);
        connection.Close();
        return dataTable;
    }

    private static void WriteToExcel(string filename, ExcelPackage excelPackage, string siteId)
    {
        //extracts label from filename
        var label = filename.Split("---").Last().Split('.').First();
        //reads script from file
        var script = File.ReadAllText(_path +_queryStoreName + @"\" + filename)
            //replaces hardcoded site with whatever it's given
            .Replace("SO1924", siteId)
            //removes the call to the stored procedure, if it is in the file
            //TODO: don't be lazy, edit your .sql files!
            .Replace("EXEC DEV.[WOC].[UPDATE_WOC_tech_tables];", "");
        //actual execution of query and saving to the _excelPackage in memory.
        var dataTable = ExecuteQueryOnDB(script);
        var worksheets = excelPackage.Workbook.Worksheets.Add(label);
        worksheets.Cells["A1"].LoadFromDataTable(dataTable, true);
        excelPackage.Save();
    }

    private static string MakeTag(string technology)
    {
        //checks if the tag it's been given is allowed, if not it returns the (ALL) tag instead
        string[] allowedTags = { "GSM", "GSM_GL", "UMTS", "LTE", "ALL", "ALL_GL" };
        var formattedTech = technology.Trim().ToUpper();
        return allowedTags.Contains(formattedTech) 
            ? @"(" + formattedTech + @")" 
            : @"(ALL)";
    }

    private static List<string> GetFileList(string tag)
    {
        //gets list of filenames to execute based on the tag it's given
        var files = new DirectoryInfo(_path + _queryStoreName).GetFiles("*.sql");
        var filteredFiles = files.Where(file => file.Name.Contains(tag)).Select(file => file.Name).ToList();
        return filteredFiles;
    }
}
