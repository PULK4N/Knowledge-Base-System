using System.ComponentModel;
using System.Globalization;
using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Models;

[TypeConverter(typeof(FileIdTypeConverter))]
public readonly record struct FileId(Guid Value)
{
    public static FileId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static FileId FromDatabaseGuid(Guid guid) =>
        new(guid);

    public override string ToString() =>
        Value.ToString("D", CultureInfo.InvariantCulture);
}

public sealed class FileIdTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(
        ITypeDescriptorContext? context,
        Type sourceType
    ) =>
        sourceType == typeof(string)
        || sourceType == typeof(Guid)
        || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value
    ) =>
        value switch
        {
            string stringValue => FileId.FromDatabaseGuid(
                Guid.Parse(stringValue)
            ),
            Guid guidValue => FileId.FromDatabaseGuid(guidValue),
            _ => base.ConvertFrom(context, culture, value)
        };

    public override bool CanConvertTo(
        ITypeDescriptorContext? context,
        Type? destinationType
    ) =>
        destinationType == typeof(string)
        || destinationType == typeof(Guid)
        || base.CanConvertTo(context, destinationType);

    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType
    )
    {
        if (value is FileId fileId)
        {
            if (destinationType == typeof(string))
                return fileId.ToString();

            if (destinationType == typeof(Guid))
                return fileId.Value;
        }

        return base.ConvertTo(
            context,
            culture,
            value,
            destinationType
        );
    }
}
