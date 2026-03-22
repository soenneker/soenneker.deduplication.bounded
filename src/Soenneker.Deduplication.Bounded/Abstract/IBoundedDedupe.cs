using System;
using System.Diagnostics.Contracts;

namespace Soenneker.Deduplication.Bounded.Abstract;

/// <summary>
/// Represents a high-performance deduplication structure backed by a bounded concurrent set.
/// Items are hashed (typically via XXH3) and stored until the set exceeds its configured size,
/// at which point older entries are evicted on a best-effort basis.
/// </summary>
/// <remarks>
/// This structure is designed for high-throughput deduplication scenarios such as:
/// <list type="bullet">
/// <item>message id dedupe</item>
/// <item>phone number suppression lists</item>
/// <item>event processing guards</item>
/// <item>rate-limiting / replay protection</item>
/// </list>
/// 
/// The underlying store is concurrent and eviction is **best-effort**, meaning the set may
/// temporarily exceed its maximum size under contention.
/// </remarks>
public interface IBoundedDedupe
{
    /// <summary>
    /// The configured maximum size of the dedupe store.
    /// </summary>
    int MaxSize { get; }

    /// <summary>
    /// Approximate number of items currently stored in the dedupe set.
    /// </summary>
    /// <remarks>
    /// This value is maintained via atomic counters for performance and may differ
    /// slightly from the exact dictionary count under heavy contention.
    /// </remarks>
    int Count { get; }

    /// <summary>
    /// Attempts to mark the specified value as seen.
    /// </summary>
    /// <param name="value">The value to hash and insert.</param>
    /// <returns>
    /// <c>true</c> if the value was not previously present and was successfully added;
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryMarkSeen(string value);

    /// <summary>
    /// Attempts to mark the specified character span as seen.
    /// </summary>
    /// <param name="value">The value to hash and insert.</param>
    /// <returns>
    /// <c>true</c> if the value was not previously present and was successfully added;
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryMarkSeen(ReadOnlySpan<char> value);

    /// <summary>
    /// Attempts to mark the specified UTF-8 payload as seen.
    /// </summary>
    /// <param name="utf8">UTF-8 encoded bytes to hash.</param>
    /// <returns>
    /// <c>true</c> if the value was not previously present and was successfully added;
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryMarkSeenUtf8(ReadOnlySpan<byte> utf8);

    /// <summary>
    /// Determines whether the specified value currently exists in the dedupe set.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is present; otherwise <c>false</c>.</returns>
    [Pure]
    bool Contains(string value);

    /// <summary>
    /// Determines whether the specified character span currently exists in the dedupe set.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is present; otherwise <c>false</c>.</returns>
    [Pure]
    bool Contains(ReadOnlySpan<char> value);

    /// <summary>
    /// Determines whether the specified UTF-8 payload currently exists in the dedupe set.
    /// </summary>
    /// <param name="utf8">UTF-8 encoded bytes to check.</param>
    /// <returns><c>true</c> if the value is present; otherwise <c>false</c>.</returns>
    [Pure]
    bool ContainsUtf8(ReadOnlySpan<byte> utf8);

    /// <summary>
    /// Attempts to remove the specified value from the dedupe set.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><c>true</c> if the value existed and was removed; otherwise <c>false</c>.</returns>
    bool TryRemove(string value);

    /// <summary>
    /// Attempts to remove the specified character span from the dedupe set.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><c>true</c> if the value existed and was removed; otherwise <c>false</c>.</returns>
    bool TryRemove(ReadOnlySpan<char> value);

    /// <summary>
    /// Attempts to remove the specified UTF-8 payload from the dedupe set.
    /// </summary>
    /// <param name="utf8">UTF-8 encoded bytes to remove.</param>
    /// <returns><c>true</c> if the value existed and was removed; otherwise <c>false</c>.</returns>
    bool TryRemoveUtf8(ReadOnlySpan<byte> utf8);
}