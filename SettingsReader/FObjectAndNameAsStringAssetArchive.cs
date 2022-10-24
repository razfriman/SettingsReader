using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace SettingsReader;

public class FObjectAndNameAsStringAssetArchive : FAssetArchive
{
    public FObjectAndNameAsStringAssetArchive(FArchive baseArchive, IPackage owner, int absoluteOffset = 0) : base(
        baseArchive, owner, absoluteOffset)
    {
    }

    public override FName ReadFName() => new FName(ReadFString());
}