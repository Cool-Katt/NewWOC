using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using Serilog;

namespace WOC;

public abstract partial class Helpers
{
    private static string _path = string.Empty;
    private static string _queryStoreName = string.Empty;
    private static string _connectionString = string.Empty;
    private static string _tag = string.Empty;
    private static string _siteId = string.Empty;
    private static ExcelPackage _excelPackage = null!;

    [GeneratedRegex(@"---(?<num>\d{0,2})(?<name>\w+)\.sql$")]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"^[A-Za-z]{2}\d{4}$")]
    private static partial Regex SiteIdRegex();

    public static void Init(IConfiguration settings, string tech, string siteId)
    {
        //a method for initializing the static class so it can have access to the applicationSettings
        _excelPackage = new ExcelPackage(new MemoryStream());
        _tag = MakeTag(tech);
        _siteId = SiteIdRegex().IsMatch(siteId)
            ? siteId.Equals(@"XX0000") ? "" : siteId
            : throw new InvalidDataException("The SiteID is not in the correct format!");
        _path = settings["QueryStoreDefaultPath"] ?? AppDomain.CurrentDomain.BaseDirectory;
        _queryStoreName = settings["QueryStoreDefaultName"] ?? "QueryStore";
        _connectionString = (MakeTag(tech).Equals(@"(CONS)")
                                ? settings["ConnectionStrings:Panorama"]
                                : settings["ConnectionStrings:Atoll"]) ??
                            throw new NullReferenceException();
    }

    public static ExcelPackage GenerateExcelFile()
    {
        //Send this command once before everything else, except when making consistency file.
        if (!_tag.Equals(@"(CONS)")) ExecuteQueryOnDB("EXEC DEV.[WOC].[UPDATE_WOC_tech_tables];");
        //Execute each selected file and write the result to _excelPackage
        foreach (var filename in GetFileList())
        {
            WriteToExcel(filename, _excelPackage);
        }

        return _excelPackage;
    }

    private static DataTable ExecuteQueryOnDB(string query)
    {
        //self explanatory
        var dataTable = new DataTable();
        using var connection = new SqlConnection(_connectionString);
        try
        {
            connection.Open();
            var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            if (reader.HasRows || _tag.Equals(@"(CONS)"))
            {
                dataTable.Load(reader);
            }
            else
            {
                //In case of empty table
                dataTable.Clear();
                dataTable.Columns.Add("Status");
                var emptyRow = dataTable.NewRow();
                emptyRow["Status"] = "The query returned no data for this siteID!";
                dataTable.Rows.Add(emptyRow);
            }
        }
        catch (SqlException exception)
        {
            Log.Error("Could not execute query:\n---\n{query}\n---\n", query);
            exception.Data.Add("query", query);
            throw;
        }
        finally
        {
            connection.Close();
        }

        return dataTable;
    }

    private static void WriteToExcel(string filename, ExcelPackage excelPackage)
    {
        //extracts label from filename
        var label = LabelRegex().Match(filename).Groups["name"].Value;
        //reads script from file
        var script = File.ReadAllText(_path + _queryStoreName + @"\" + filename)
            //replaces hardcoded site with whatever it's given
            .Replace("SO1924", _siteId)
            .Replace("@SiteID@", $"'{_siteId}'")
            .Replace("EXEC DEV.[WOC].[UPDATE_WOC_tech_tables];", "");
        //actual execution of query and saving to the _excelPackage in memory.
        var dataTable = ExecuteQueryOnDB(script);
        var worksheets = excelPackage.Workbook.Worksheets.Add(label);
        worksheets.Cells["A1"].LoadFromDataTable(dataTable, true);
        //setting column width and timestamp formatting
        worksheets.Cells[worksheets.Dimension.Address].AutoFitColumns();
        if (worksheets.Cells["A1"].Value.Equals("timestamp"))
            worksheets.Cells["A:A"].Style.Numberformat.Format = DateTimeFormatInfo.CurrentInfo.ShortDatePattern;
        excelPackage.Save();
    }

    private static string MakeTag(string technology)
    {
        //checks if the tag it's been given is allowed, if not it returns the (ALL) tag instead
        string[] allowedTags = { "GSM", "GSM_GL", "UMTS", "LTE", "ALL", "ALL_GL", "CONS" };
        var formattedTech = technology.Trim().ToUpper();
        return allowedTags.Contains(formattedTech)
            ? @"(" + formattedTech + @")"
            : @"(ALL)";
    }

    private static List<string> GetFileList()
    {
        //gets list of filenames to execute based on the tag it's given
        var files = new DirectoryInfo(_path + _queryStoreName).GetFiles("*.sql");
        var filteredFiles = files.Where(file => file.Name.Contains(_tag)).Select(file => file.Name).ToList();
        filteredFiles.Sort();
        return filteredFiles;
    }
}