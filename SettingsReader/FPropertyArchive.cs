using System.IO.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Readers;

namespace SettingsReader;

public class FPropertyArchive
{
    private const int MagicNumber = 0x44464345;

    public string? Branch { get; set; }
    public List<FCustomVersion> CustomVersions { get; set; } = new();
    public FStructFallback Root { get; set; } = new();

    public async Task Deserialize(Stream stream) =>
        await Deserialize(new FStreamArchive("name", stream));

    public async Task Deserialize(byte[] data) =>
        await Deserialize(new FByteArchive("name", data));

    public async Task Deserialize(FArchive archive)
    {
        var magic = archive.Read<uint>();

        if (!magic.Equals(MagicNumber))
        {
            throw new NotImplementedException("Invalid settings file.");
        }

        archive.Seek(4, SeekOrigin.Current);

        var isCompressed = archive.ReadBoolean();
        if (!isCompressed)
        {
            throw new NotImplementedException("File is not compressed");
        }

        var decompressedLength = archive.Read<int>();
        var remaining = (int)(archive.Length - archive.Position);
        var compressedStream = new MemoryStream(archive.ReadBytes(remaining));
        var decompressedStream = await DecompressData(compressedStream, decompressedLength);
        var decompressedReader = new FByteArchive("decompressed", decompressedStream.ToArray());
        archive = decompressedReader;
        var something = archive.Read<short>();
        archive.Seek(2, SeekOrigin.Current);
        archive.Read<uint>();
        if (something != 2464)
        {
            archive.Read<int>();
        }

        archive.Seek(4, SeekOrigin.Current);
        archive.Read<short>();
        archive.Read<int>();
        Branch = archive.ReadFString();
        archive.Read<int>();
        archive.Read<byte>();
        archive.Read<byte>();
        CustomVersions = archive.ReadArray<FCustomVersion>().ToList();

        using var fileProvider = new StreamedFileProvider("");
        var typeMappings = new TypeMappings();
        var package = new EmptyPackage("empty", fileProvider, typeMappings);
        await using var objectArchive = new FObjectAndNameAsStringAssetArchive(archive, package);
        var propertyStruct = new FStructFallback(objectArchive, "None");
        Root = propertyStruct;
    }

    public static async Task<FArchive> Decompress(FArchive reader)
    {
        var magicNumber = reader.Read<int>();
        if (magicNumber != MagicNumber)
        {
            throw new InvalidDataException("Invalid settings file");
        }

        reader.Read<int>();
        reader.Read<int>();
        var decompressedLength = reader.Read<int>();
        var remaining = (int)(reader.Length - reader.Position);
        var compressedStream = new MemoryStream(reader.ReadBytes(remaining));
        var decompressedStream = await DecompressData(compressedStream, decompressedLength);

        var decompressedReader = new FByteArchive("decompressed", decompressedStream.ToArray());
        return decompressedReader;
    }

    private static async Task<MemoryStream> DecompressData(Stream compressedStream, int length)
    {
        var decompressedStream = new MemoryStream(length);
        await using var inflater = new ZLibStream(compressedStream, CompressionMode.Decompress);
        await inflater.CopyToAsync(decompressedStream);
        decompressedStream.Seek(0, SeekOrigin.Begin);
        return decompressedStream;
    }
}