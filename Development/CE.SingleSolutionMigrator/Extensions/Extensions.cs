// Extensions.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

namespace CE.SingleSolutionMigrator.Extensions
{
    public static class Extensions
    {
        public static string ToFormattedGuid(this Guid guid)
        {
            return $"{{{guid}}}".ToUpperInvariant();
        }
    }
}
