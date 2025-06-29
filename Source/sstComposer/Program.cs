/*******************************************************************************
*
*  (C) COPYRIGHT AUTHORS, 2014 - 2025
*
*  TITLE:       PROGRAM.CS
*
*  VERSION:     2.10
*
*  DATE:        29 Jun 2025
*
*  SSTC entrypoint - System Service Table Composer
* 
* THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
* ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED
* TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
* PARTICULAR PURPOSE.
*
*******************************************************************************/

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sstc;

public enum TableType { Ntos, Win32k, Ium }
public enum OutputFormat { Markdown, Html, Csv, Json }

public class SyscallEntry
{
    public required string ServiceName { get; set; }
    public Dictionary<string, int> TableIndexes { get; set; } = new();
}

public class CommandLineArguments
{
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Markdown;
    public TableType TableType { get; set; } = TableType.Ntos;
    public string TablesDirectory { get; set; } = "tables";
    public bool ValidateOnly { get; set; }
    public bool Verbose { get; set; }

    public static CommandLineArguments? Parse(string[] args)
    {
        var result = new CommandLineArguments();
        bool hasTableTypeSpecified = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLowerInvariant();

            switch (arg)
            {
                case "-h":
                case "--html":
                    result.OutputFormat = OutputFormat.Html;
                    break;
                case "-c":
                case "--csv":
                    result.OutputFormat = OutputFormat.Csv;
                    break;
                case "-j":
                case "--json":
                    result.OutputFormat = OutputFormat.Json;
                    break;
                case "-w":
                case "--win32k":
                    if (hasTableTypeSpecified && result.TableType != TableType.Win32k)
                    {
                        Console.WriteLine("Error: Table type parameters are mutually exclusive");
                        return null;
                    }
                    result.TableType = TableType.Win32k;
                    hasTableTypeSpecified = true;
                    break;
                case "-ium":
                case "--ium":
                    if (hasTableTypeSpecified && result.TableType != TableType.Ium)
                    {
                        Console.WriteLine("Error: Table type parameters are mutually exclusive");
                        return null;
                    }
                    result.TableType = TableType.Ium;
                    hasTableTypeSpecified = true;
                    break;
                case "-d":
                case "--dir":
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        result.TablesDirectory = args[++i];
                    }
                    else
                    {
                        Console.WriteLine("-d found but input tables directory is not specified, default will be used");
                    }
                    break;
                case "--validate":
                    result.ValidateOnly = true;
                    break;
                case "-v":
                case "--verbose":
                    result.Verbose = true;
                    break;
                default:
                    Console.WriteLine($"Unrecognized command \"{arg}\"");
                    return null;
            }
        }

        return result;
    }

    public static void ShowHelp()
    {
        Console.WriteLine("sstc [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -d, --dir <path>  - Specify tables directory to combine, default value \"tables\"");
        Console.WriteLine("  -h, --html        - Output result as HTML table");
        Console.WriteLine("  -c, --csv         - Output result as CSV file");
        Console.WriteLine("  -j, --json        - Output result as JSON file");
        Console.WriteLine("  -w, --win32k      - Combine win32k syscalls, default is ntos");
        Console.WriteLine("  -ium, --ium       - Combine ium syscalls");
        Console.WriteLine("  --validate        - Validate table files without generating output");
        Console.WriteLine("  -v, --verbose     - Show detailed processing information");
        Console.WriteLine("  Note: Default output format is Markdown if not specified otherwise");
    }
}

public class SyscallTableProcessor
{
    private readonly string[] _tableFiles;
    private readonly List<SyscallEntry> _entries = new();
    private readonly List<string> _tableVersions = new();
    private readonly bool _verbose;
    private int _entriesProcessed = 0;
    private int _linesSkipped = 0;

    public SyscallTableProcessor(string[] tableFiles, bool verbose)
    {
        _tableFiles = tableFiles ?? throw new ArgumentNullException(nameof(tableFiles));
        _verbose = verbose;

        Array.Sort(_tableFiles, (a, b) =>
        {
            Match matchA = Regex.Match(Path.GetFileNameWithoutExtension(a), @"\d+");
            Match matchB = Regex.Match(Path.GetFileNameWithoutExtension(b), @"\d+");

            if (matchA.Success && matchB.Success)
            {
                int av = int.Parse(matchA.Value);
                int bv = int.Parse(matchB.Value);
                return av.CompareTo(bv);
            }

            return string.Compare(Path.GetFileName(a), Path.GetFileName(b));
        });
    }

    public bool ProcessFiles()
    {
        var sw = Stopwatch.StartNew();

        foreach (string tablePath in _tableFiles)
        {
            string tableVersion = Path.GetFileNameWithoutExtension(tablePath);
            _tableVersions.Add(tableVersion);

            try
            {
                string[] lines = File.ReadAllLines(tablePath);
                if (_verbose)
                    Console.WriteLine($"Processing {tablePath} with {lines.Length} entries");

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        _linesSkipped++;
                        continue;
                    }

                    int tabIndex = line.IndexOf('\t');
                    if (tabIndex <= 0)
                    {
                        if (_verbose)
                            Console.WriteLine($"Warning: Invalid line format in {tablePath}: {line}");
                        _linesSkipped++;
                        continue;
                    }

                    string serviceName = line[..tabIndex];
                    if (!int.TryParse(line[(tabIndex + 1)..], out int syscallId))
                    {
                        if (_verbose)
                            Console.WriteLine($"Warning: Invalid syscall ID in {tablePath}: {line}");
                        _linesSkipped++;
                        continue;
                    }

                    var entry = FindOrCreateEntry(serviceName);
                    entry.TableIndexes[tableVersion] = syscallId;
                    _entriesProcessed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {tablePath}: {ex.Message}");
                return false;
            }
        }

        sw.Stop();
        if (_verbose)
        {
            Console.WriteLine($"Processing completed in {sw.ElapsedMilliseconds}ms:");
            Console.WriteLine($"  - Files processed: {_tableFiles.Length}");
            Console.WriteLine($"  - Unique services found: {_entries.Count}");
            Console.WriteLine($"  - Total entries processed: {_entriesProcessed}");
            Console.WriteLine($"  - Lines skipped: {_linesSkipped}");
        }

        return true;
    }

    private SyscallEntry FindOrCreateEntry(string serviceName)
    {
        int low = 0, high = _entries.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            int comparison = string.Compare(_entries[mid].ServiceName, serviceName, StringComparison.Ordinal);

            if (comparison == 0)
                return _entries[mid];
            else if (comparison < 0)
                low = mid + 1;
            else
                high = mid - 1;
        }

        var newEntry = new SyscallEntry { ServiceName = serviceName };
        _entries.Insert(low, newEntry);
        return newEntry;
    }

    public void WriteMarkdownTable(string outputPath)
    {
        var header = new StringBuilder("|#|ServiceName|");
        foreach (string version in _tableVersions) header.Append(version).Append('|');

        var divider = new StringBuilder("|---|---|");
        for (int i = 0; i < _tableVersions.Count; i++) divider.Append("---|");

        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        writer.WriteLine(header);
        writer.WriteLine(divider);

        int index = 1;
        foreach (var entry in _entries)
        {
            var row = new StringBuilder($"|{index}|{entry.ServiceName}|");
            foreach (string version in _tableVersions)
            {
                if (entry.TableIndexes.TryGetValue(version, out int syscallId))
                    row.Append(syscallId);
                row.Append('|');
            }
            writer.WriteLine(row);
            index++;
        }

        Console.WriteLine($"Markdown table written to {outputPath}");
    }

    public void WriteCsvTable(string outputPath)
    {
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        writer.Write("Index,ServiceName");
        foreach (string version in _tableVersions)
        {
            writer.Write(",");
            writer.Write(version);
        }
        writer.WriteLine();

        int index = 1;
        foreach (var entry in _entries)
        {
            writer.Write($"{index},{EscapeCsvField(entry.ServiceName)}");
            foreach (string version in _tableVersions)
            {
                writer.Write(",");
                if (entry.TableIndexes.TryGetValue(version, out int syscallId))
                    writer.Write(syscallId);
            }
            writer.WriteLine();
            index++;
        }

        Console.WriteLine($"CSV table written to {outputPath}");
    }

    private static string EscapeCsvField(string field) =>
        (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;

    private Dictionary<string, string> ComputeFirstSeenVersions()
    {
        var firstSeen = new Dictionary<string, string>();
        foreach (var entry in _entries)
        {
            foreach (var version in _tableVersions)
            {
                if (entry.TableIndexes.ContainsKey(version))
                {
                    if (!firstSeen.ContainsKey(entry.ServiceName))
                        firstSeen[entry.ServiceName] = version;
                    break;
                }
            }
        }
        return firstSeen;
    }

    public void WriteHtmlTable(string outputPath, TableType tableType)
    {
        const string css = "body{font-family:\"Segoe UI\",-apple-system,BlinkMacSystemFont,Roboto,\"Helvetica Neue\",sans-serif;line-height:1.4;color:#333;background-color:#fff;padding:0 5vw}table{margin:1em 0;border-collapse:collapse;border:.1em solid #d6d6d6;width:100%}caption{text-align:left;padding:.25em .5em .5em;font-size:1.2em;font-weight:700}th,td{padding:.25em .5em .25em 1em;vertical-align:text-top;text-align:left;text-indent:-.5em;border:.1em solid #d6d6d6}th{vertical-align:bottom;background-color:#666;color:#fff;position:sticky;top:0;z-index:2;cursor:pointer}tr:nth-child(even){background-color:rgba(0,0,0,.05)}tr:nth-child(odd){background-color:rgba(255,255,255,.05)}th[scope=row]{position:sticky;left:0;z-index:1;vertical-align:top;color:inherit;background-color:inherit;background:linear-gradient(90deg,transparent 0,transparent calc(100% - .05em),#d6d6d6 calc(100% - .05em),#d6d6d6 100%)}table:nth-of-type(2) th:not([scope=row]):first-child{left:0;z-index:3;background:linear-gradient(90deg,#666 0,#666 calc(100% - .05em),#ccc calc(100% - .05em),#ccc 100%)}th[scope=row]+td{min-width:24em}th[scope=row]{min-width:20em}body{padding-bottom:90vh}.search-box{margin:1em 0;padding:.5em;width:100%;max-width:500px;font-size:1em;border:1px solid #ccc;border-radius:4px}.filter-container{display:flex;flex-wrap:wrap;gap:10px;margin:15px 0}.filter-item{cursor:pointer;padding:5px 10px;background-color:#eee;border-radius:4px;font-size:.9em}.filter-item.active{background-color:#666;color:#fff}.filter-button{cursor:pointer;padding:5px 10px;background-color:#555;color:#fff;border:none;border-radius:4px;margin-left:10px}th.filtered{background-color:#4b9e4a}.col-hidden{display:none}.highlight-diff{background-color:rgba(255,235,59,.3)!important;font-weight:700;color:#d32f2f}.highlight-same{background-color:rgba(76,175,80,.2)!important}";
        const string js = "function filterTable(){var t,e,n,l,r,i,o;for(t=document.getElementById(\"searchInput\"),e=t.value.toUpperCase(),n=document.querySelector(\"table\"),l=n.getElementsByTagName(\"tr\"),i=1;i<l.length;i++)(r=l[i].getElementsByTagName(\"td\")[1])&&((o=r.textContent||r.innerText).toUpperCase().indexOf(e)>-1?l[i].style.display=\"\":l[i].style.display=\"none\")}var activeColumns=[];function toggleColumnFilter(t){var e=activeColumns.indexOf(t);e>-1?activeColumns.splice(e,1):activeColumns.push(t);var n=document.querySelectorAll(\"table th\");if(0===activeColumns.length){for(var l=2;l<n.length;l++)n[l].classList.remove(\"filtered\"),toggleColumnVisibility(l,!0);return void clearHighlighting()}for(l=2;l<n.length;l++){var r=activeColumns.includes(l);n[l].classList.toggle(\"filtered\",r),toggleColumnVisibility(l,r)}highlightDifferences()}function toggleColumnVisibility(t,e){for(var n=document.querySelector(\"table\"),l=n.getElementsByTagName(\"tr\"),r=0;r<l.length;r++){var i=0===r?l[r].getElementsByTagName(\"th\"):l[r].getElementsByTagName(\"td\");t<i.length&&(e?i[t].classList.remove(\"col-hidden\"):i[t].classList.add(\"col-hidden\"))}}function highlightDifferences(){if(!(activeColumns.length<=1)){for(var t=document.querySelector(\"table\"),e=t.getElementsByTagName(\"tr\"),n=1;n<e.length;n++){for(var l=e[n].getElementsByTagName(\"td\"),r=[],i=0;i<activeColumns.length;i++){var o=activeColumns[i];if(o<l.length){var s=l[o].textContent.trim();s&&r.push(s)}}var a=r.length>0;for(i=1;i<r.length;i++)if(r[i]!==r[0]){a=!1;break}for(i=0;i<activeColumns.length;i++)if((o=activeColumns[i])<l.length&&l[o].textContent.trim()){l[o].classList.remove(\"highlight-diff\",\"highlight-same\"),a?r.length>1&&l[o].classList.add(\"highlight-same\"):l[o].classList.add(\"highlight-diff\")}}}}function clearHighlighting(){document.querySelectorAll(\".highlight-diff, .highlight-same\").forEach(function(t){t.classList.remove(\"highlight-diff\",\"highlight-same\")})}function setupColumnFilters(){var t=document.querySelectorAll(\"table th\"),e=document.getElementById(\"filterContainer\");for(var n=2;n<t.length;n++){var l=t[n].textContent,r=document.createElement(\"span\");r.className=\"filter-item\",r.textContent=l,r.setAttribute(\"data-column\",n),r.onclick=function(){this.classList.toggle(\"active\"),toggleColumnFilter(parseInt(this.getAttribute(\"data-column\")))},e.appendChild(r)}var i=document.createElement(\"button\");i.className=\"filter-button\",i.textContent=\"Show All\",i.onclick=function(){for(var e=document.querySelectorAll(\".filter-item\"),n=0;n<e.length;n++)e[n].classList.remove(\"active\");activeColumns=[];for(var l=2;l<t.length;l++)t[l].classList.remove(\"filtered\"),toggleColumnVisibility(l,!0);clearHighlighting()},e.appendChild(i)}window.onload=setupColumnFilters;";

        var firstSeen = ComputeFirstSeenVersions();
        var grouped = _tableVersions
            .Select(version => _entries
                .Where(e => firstSeen[e.ServiceName] == version)
                .OrderBy(e => e.ServiceName)
                .ToList())
            .ToList();

        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

        string tableTitle = tableType switch
        {
            TableType.Ium => "IUM System Service Table",
            TableType.Win32k => "Win32k System Service Table",
            _ => "NT OS System Service Table",
        };

        writer.Write("<!DOCTYPE html><html><head><title>");
        writer.Write(tableTitle);
        writer.Write("</title><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><style>");
        writer.Write(css);
        writer.Write("</style><script>");
        writer.Write(js);
        writer.Write("</script></head><body>");

        writer.Write("<input type=\"text\" id=\"searchInput\" class=\"search-box\" onkeyup=\"filterTable()\" placeholder=\"Search for syscalls...\">");
        writer.Write("<div id=\"filterContainer\" class=\"filter-container\">");
        writer.Write("<strong>Select:</strong>");
        writer.Write("</div>");

        writer.Write($"<table><caption>{tableTitle}</caption><tr><th>#</th><th>ServiceName</th>");
        foreach (string version in _tableVersions)
            writer.Write($"<th>{version}</th>");
        writer.Write("</tr>");

        int index = 1;
        foreach (var group in grouped)
        {
            foreach (var entry in group)
            {
                writer.Write($"<tr><td>{index}</td><td>{entry.ServiceName}</td>");
                foreach (string v in _tableVersions)
                {
                    writer.Write("<td>");
                    if (entry.TableIndexes.TryGetValue(v, out int syscallId))
                        writer.Write(syscallId);
                    writer.Write("</td>");
                }
                writer.Write("</tr>");
                index++;
            }
        }

        writer.Write("</table></body></html>");
        Console.WriteLine($"HTML table written to {outputPath}");
    }

    public void WriteJsonTable(string outputPath)
    {
        var output = new
        {
            Builds = _tableVersions,
            Syscalls = _entries.Select(entry => new
            {
                Name = entry.ServiceName,
                Ids = _tableVersions.Select(ver =>
                    entry.TableIndexes.TryGetValue(ver, out int id) ? id : (int?)null
                ).ToArray()
            }).ToList()
        };

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(output, jsonOptions));
        Console.WriteLine($"JSON table written to {outputPath}");
    }
}

class Program
{
    static int Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("SSTC - System Service Table Composer");

        var assembly = Assembly.GetEntryAssembly();
        var hashId = assembly?.ManifestModule?.ModuleVersionId.ToString() ?? "<unknown>";
        Console.WriteLine($"Build MVID: {hashId}");

        if (args.Length == 0)
        {
            CommandLineArguments.ShowHelp();
            return 1;
        }

        var options = CommandLineArguments.Parse(args);
        if (options == null)
            return 2;

        string subDir = options.TableType switch
        {
            TableType.Win32k => "win32k",
            TableType.Ium => "ium",
            _ => "ntos"
        };

        string lookupPath = Path.Combine(Directory.GetCurrentDirectory(), options.TablesDirectory, subDir);

        string[] tableFiles;
        try
        {
            tableFiles = Directory.GetFiles(lookupPath, "*.txt");
            if (tableFiles.Length == 0)
            {
                Console.WriteLine($"No table files found in {lookupPath}");
                return 3;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing directory {lookupPath}: {ex.Message}");
            return 4;
        }

        var processor = new SyscallTableProcessor(tableFiles, options.Verbose);
        if (!processor.ProcessFiles())
        {
            Console.WriteLine("Failed to process syscall tables.");
            return 5;
        }

        if (options.ValidateOnly)
        {
            Console.WriteLine("Table files validated successfully.");
            return 0;
        }

        string outputBaseName = options.TableType switch
        {
            TableType.Win32k => "w32ksyscalls",
            TableType.Ium => "iumsyscalls",
            _ => "syscalls",
        };

        try
        {
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(),
                options.OutputFormat switch
                {
                    OutputFormat.Html => $"{outputBaseName}.html",
                    OutputFormat.Csv => $"{outputBaseName}.csv",
                    OutputFormat.Json => $"{outputBaseName}.json",
                    _ => $"{outputBaseName}.md"
                });

            switch (options.OutputFormat)
            {
                case OutputFormat.Html:
                    processor.WriteHtmlTable(outputPath, options.TableType);
                    break;
                case OutputFormat.Csv:
                    processor.WriteCsvTable(outputPath);
                    break;
                case OutputFormat.Json:
                    processor.WriteJsonTable(outputPath);
                    break;
                default:
                    processor.WriteMarkdownTable(outputPath);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing output file: {ex.Message}");
            return 6;
        }

        return 0;
    }
}
