using UnityEngine;
using System.Collections;
using System;

namespace Sabresaurus.Sidekick
{
    public static class SidekickExtensions
    {
        public static RectOffset SetLeft(this RectOffset rectOffset, int newValue)
        {
            rectOffset.left = newValue;
            return rectOffset;
        }

        public static RectOffset SetRight(this RectOffset rectOffset, int newValue)
        {
            rectOffset.right = newValue;
            return rectOffset;
        }

		public static bool Contains(this string source, string value, StringComparison comparison)
		{
			return source.IndexOf(value, comparison) >= 0;
		}

        public static bool HasFlagByte(this Enum mask, Enum flags) // Same behavior than Enum.HasFlag is .NET 4
        {
            return ((byte)(IConvertible)mask & (byte)(IConvertible)flags) == (byte)(IConvertible)flags;
        }
    }
}