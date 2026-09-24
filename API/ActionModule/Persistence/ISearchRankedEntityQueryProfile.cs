using System.Linq.Expressions;

namespace ActionModule.Persistence;

/// <summary>
/// Optional profile extension that places primary search matches, such as
/// title matches, ahead of other matches. The requested sort still orders
/// rows inside each group.
/// </summary>
public interface ISearchRankedEntityQueryProfile<TEntry>
{
    Expression<Func<TEntry, bool>> IsPrimarySearchMatch(string search);
}
