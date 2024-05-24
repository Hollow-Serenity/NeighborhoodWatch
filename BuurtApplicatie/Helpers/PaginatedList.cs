using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BuurtApplicatie.Helpers
{
    public class PaginatedList<T> : List<T>
    {
        public int Page { get; private set; }
        public int PageCount { get; private set; }

        public PaginatedList(List<T> partialList, int total, int page, int perPage)
        {
            Page = page;
            PageCount = (int) Math.Ceiling(total / (double) perPage);
            this.AddRange(partialList);
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> list, int page, int perPage)
        {
            return new PaginatedList<T>(
                await list.Skip(page * perPage).Take(perPage).ToListAsync(),
                await list.CountAsync(),
                page,
                perPage);
        }

        public bool HasPrevious()
        {
            return Page > 0;
        }

        public bool HasNext()
        {
            return Page < PageCount - 1;
        }
    }
}