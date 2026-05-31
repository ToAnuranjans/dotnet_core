using System.Collections;

namespace MiddleMan.Serialization;

internal static class JsonTypeHelpers
{
    public static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    public static bool IsSimpleType(Type type)
    {
        var concreteType = Nullable.GetUnderlyingType(type) ?? type;
        return concreteType.IsPrimitive
            || concreteType.IsEnum
            || concreteType == typeof(string)
            || concreteType == typeof(decimal)
            || concreteType == typeof(DateTime)
            || concreteType == typeof(DateTimeOffset)
            || concreteType == typeof(Guid)
            || concreteType == typeof(TimeSpan);
    }

    public static bool TryGetCollectionItemType(Type type, out Type? itemType)
    {
        itemType = null;
        if (type == typeof(string) || TryGetDictionaryTypes(type, out _, out _))
        {
            return false;
        }

        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return itemType is not null;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            itemType = type.GetGenericArguments()[0];
            return true;
        }

        var enumerableType = type.GetInterfaces()
            .FirstOrDefault(interfaceType => interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        itemType = enumerableType?.GetGenericArguments()[0];
        return itemType is not null;
    }

    public static bool TryGetDictionaryTypes(Type type, out Type? keyType, out Type? valueType)
    {
        keyType = null;
        valueType = null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            keyType = type.GetGenericArguments()[0];
            valueType = type.GetGenericArguments()[1];
            return true;
        }

        var dictionaryType = type.GetInterfaces()
            .FirstOrDefault(interfaceType => interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictionaryType is null)
        {
            return false;
        }

        keyType = dictionaryType.GetGenericArguments()[0];
        valueType = dictionaryType.GetGenericArguments()[1];
        return true;
    }
}
