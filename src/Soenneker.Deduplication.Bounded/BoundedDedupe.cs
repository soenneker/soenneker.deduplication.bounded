using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Soenneker.Deduplication.Bounded.Abstract;
using Soenneker.Hashing.XxHash;
using Soenneker.Sets.Concurrent.Bounded;

namespace Soenneker.Deduplication.Bounded;

/// <inheritdoc cref="IBoundedDedupe"/>
public sealed class BoundedDedupe : IBoundedDedupe
{
    private readonly BoundedConcurrentSet<ulong> _set;

    private readonly long _seed;

    public BoundedDedupe(int maxSize, int capacityHint = 0, long seed = 0, int trimBatchSize = 64, int trimStartOveragePercent = 5,
        int maxTrimWorkPerCall = 4096, int resyncAfterNoProgress = 8, int queueOverageFactor = 4)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize));

        _seed = seed;

        _set = new BoundedConcurrentSet<ulong>(maxSize, capacityHint: capacityHint, trimBatchSize: trimBatchSize,
            trimStartOveragePercent: trimStartOveragePercent, maxTrimWorkPerCall: maxTrimWorkPerCall, resyncAfterNoProgress: resyncAfterNoProgress,
            queueOverageFactor: queueOverageFactor);
    }

    public int MaxSize => _set.MaxSize;

    public int Count => _set.ApproxCount; // approx, but stable enough for diagnostics

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryMarkSeen(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return TryMarkSeen(value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryMarkSeen(ReadOnlySpan<char> value) => _set.TryAdd(XxHash3Util.HashCharsToUInt64(value, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryMarkSeenUtf8(ReadOnlySpan<byte> utf8) => _set.TryAdd(XxHash3Util.HashUtf8ToUInt64(utf8, _seed));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return Contains(value.AsSpan());
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ReadOnlySpan<char> value) => _set.Contains(XxHash3Util.HashCharsToUInt64(value, _seed));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsUtf8(ReadOnlySpan<byte> utf8) => _set.Contains(XxHash3Util.HashUtf8ToUInt64(utf8, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemove(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return TryRemove(value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemove(ReadOnlySpan<char> value) => _set.TryRemove(XxHash3Util.HashCharsToUInt64(value, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemoveUtf8(ReadOnlySpan<byte> utf8) => _set.TryRemove(XxHash3Util.HashUtf8ToUInt64(utf8, _seed));
}