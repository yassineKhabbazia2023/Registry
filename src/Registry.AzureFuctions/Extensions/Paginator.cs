// <copyright file="Paginator.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Registry.AzureFuctions
{
    public static class Paginator
    {
        public static int GetTotalPages(int totalItems, int pageSize)
        {
            if (totalItems <= 0 || pageSize <= 0)
            {
                return 1;
            }

            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return totalPages;
        }

        public static int GetValidPageNumber(int pageNumber)
        {
            return Math.Max(1, pageNumber);
        }

        public static int GetValidPageSize(int pageSize)
        {
            return pageSize <= 0 ? int.MaxValue : pageSize;
        }
    }
}
