using System.Text;
using ScrivenerSync.Domain.Interfaces.Services;
using ScrivenerSync.Infrastructure.Parsing;

// ---------------------------------------------------------------------------
// ScrivenerSync DevTools - Scrivener project parser test harness
// ---------------------------------------------------------------------------

var scrivPath = args.Length > 0
    ? args[0]
    : @"C:\Users\alast\Dropbox\Apps\Scrivener\Test.scriv";

Console.OutputEncoding = Encoding.UTF8;

Banner("ScrivenerSync DevTools");
Console.WriteLine($"Target: {scrivPath}");
Console.WriteLine();

if (!Directory.Exists(scrivPath))
{
    Error($"Directory not found: {scrivPath}");
    return 1;
}

var scrivxFiles = Directory.GetFiles(scrivPath, "*.scrivx");
if (scrivxFiles.Length == 0)
{
    Error("No .scrivx file found in the target directory.");
    return 1;
}

var scrivxPath = scrivxFiles[0];
Console.WriteLine($"Found: {Path.GetFileName(scrivxPath)}");
Console.WriteLine();

// ---------------------------------------------------------------------------
// Step 1: Parse project.scrivx
// ---------------------------------------------------------------------------
Banner("Step 1: Parsing project.scrivx");

ParsedProject parsed;
try
{
    var parser = new ScrivenerProjectParser();
    parsed = parser.Parse(scrivxPath);
    Success("Parsed successfully.");
}
catch (Exception ex)
{
    Error($"Parse failed: {ex.Message}");
    return 1;
}

// ---------------------------------------------------------------------------
// Step 2: Show status map
// ---------------------------------------------------------------------------
Banner("Step 2: Status map");
if (parsed.StatusMap.Count == 0)
{
    Console.WriteLine("  (no status items found)");
}
else
{
    foreach (var kvp in parsed.StatusMap.OrderBy(k => k.Key))
        Console.WriteLine($"  [{kvp.Key,3}] {kvp.Value}");
}
Console.WriteLine();

// ---------------------------------------------------------------------------
// Step 3: Print binder tree
// ---------------------------------------------------------------------------
Banner("Step 3: Binder tree (Manuscript only)");
if (parsed.ManuscriptRoot is null)
{
    Error("No DraftFolder (Manuscript) found.");
    return 1;
}

PrintTree(parsed.ManuscriptRoot, indent: 0);
Console.WriteLine();

// ---------------------------------------------------------------------------
// Step 4: Count nodes
// ---------------------------------------------------------------------------
Banner("Step 4: Node summary");
var allNodes  = Flatten(parsed.ManuscriptRoot).ToList();
var folders   = allNodes.Count(n => n.NodeType == ParsedNodeType.Folder);
var documents = allNodes.Count(n => n.NodeType == ParsedNodeType.Document);
Console.WriteLine($"  Total nodes : {allNodes.Count}");
Console.WriteLine($"  Folders     : {folders}");
Console.WriteLine($"  Documents   : {documents}");
Console.WriteLine();

// ---------------------------------------------------------------------------
// Step 5: Convert first document node to HTML
// ---------------------------------------------------------------------------
Banner("Step 5: RTF conversion (first Document node)");

var firstDoc = allNodes.FirstOrDefault(n => n.NodeType == ParsedNodeType.Document);
if (firstDoc is null)
{
    Console.WriteLine("  No document nodes found.");
}
else
{
    Console.WriteLine($"  Converting: {firstDoc.Title} ({firstDoc.Uuid})");
    try
    {
        var converter = new RtfConverter();
        var result    = await converter.ConvertAsync(scrivPath, firstDoc.Uuid);

        if (result is null)
        {
            Console.WriteLine("  No content.rtf found for this node (empty document).");
        }
        else
        {
            Success("Converted successfully.");
            Console.WriteLine($"  Hash : {result.Hash}");
            Console.WriteLine($"  HTML ({result.Html.Length} chars):");
            Console.WriteLine();

            var preview = result.Html.Length > 800
                ? result.Html[..800] + "..."
                : result.Html;
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(preview);
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Error($"Conversion failed: {ex.Message}");
    }
}

Console.WriteLine();

// ---------------------------------------------------------------------------
// Step 6: Batch conversion summary
// ---------------------------------------------------------------------------
Banner("Step 6: Batch conversion summary");
Console.WriteLine($"  Converting {documents} document node(s)...");
Console.WriteLine();

var converter2   = new RtfConverter();
var successCount = 0;
var emptyCount   = 0;
var failCount    = 0;

foreach (var doc in allNodes.Where(n => n.NodeType == ParsedNodeType.Document))
{
    try
    {
        var result = await converter2.ConvertAsync(scrivPath, doc.Uuid);
        if (result is null)
        {
            emptyCount++;
            Console.WriteLine($"  [EMPTY ] {doc.Title}");
        }
        else
        {
            successCount++;
            Console.WriteLine($"  [OK    ] {doc.Title} - {result.Html.Length} chars, hash: {result.Hash[..12]}...");
        }
    }
    catch (Exception ex)
    {
        failCount++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [FAILED] {doc.Title} - {ex.Message}");
        Console.ResetColor();
    }
}

Console.WriteLine();
Console.WriteLine($"  Converted : {successCount}");
Console.WriteLine($"  Empty     : {emptyCount}");
Console.WriteLine($"  Failed    : {failCount}");
Console.WriteLine();

if (failCount == 0)
    Success("All conversions completed without errors.");
else
    Error($"{failCount} conversion(s) failed.");

return failCount > 0 ? 1 : 0;

// ---------------------------------------------------------------------------
// Helper functions
// ---------------------------------------------------------------------------

void PrintTree(ParsedBinderNode node, int indent)
{
    var prefix  = new string(' ', indent * 2);
    var typeTag = node.NodeType == ParsedNodeType.Folder ? "[F]" : "[D]";
    var status  = node.ScrivenerStatus is not null ? $" ({node.ScrivenerStatus})" : "";
    Console.WriteLine($"{prefix}{typeTag} {node.Title}{status}");
    foreach (var child in node.Children)
        PrintTree(child, indent + 1);
}

IEnumerable<ParsedBinderNode> Flatten(ParsedBinderNode node)
{
    yield return node;
    foreach (var child in node.Children)
        foreach (var n in Flatten(child))
            yield return n;
}

void Banner(string text)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"--- {text} ---");
    Console.ResetColor();
}

void Success(string text)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  {text}");
    Console.ResetColor();
}

void Error(string text)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  ERROR: {text}");
    Console.ResetColor();
}
