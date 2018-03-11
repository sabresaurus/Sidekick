using System;

public static class EnumHelper
{
    public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct
    {
        if (Enum.IsDefined(typeof(TEnum), value))
        {
            result = (TEnum)Enum.Parse(typeof(TEnum), value);
            return true;
        }
        else
        {
            result = default(TEnum);
            return false;
        }
    }
}
