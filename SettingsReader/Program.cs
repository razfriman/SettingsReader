using Newtonsoft.Json;
using SettingsReader;

var inputFile = "ClientSettings.sav";
var outputFile = "ClientSettings.json";

var archive = new FPropertyArchive();
var bytes = await File.ReadAllBytesAsync(inputFile);
await archive.Deserialize(bytes);

var serializer = JsonSerializer.Create(new JsonSerializerSettings
{
    Formatting = Formatting.Indented
});
await using var writer = new StringWriter();
serializer.Serialize(writer, archive.Root);
var json = writer.ToString();

await File.WriteAllTextAsync(outputFile, json);
Console.WriteLine(json);