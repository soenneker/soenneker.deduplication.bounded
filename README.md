[![](https://img.shields.io/nuget/v/soenneker.deduplication.bounded.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.bounded/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.bounded/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.bounded/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.deduplication.bounded.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.bounded/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Deduplication.Bounded

### A thread-safe high-performance bounded size deduplication utility for .NET.

## Installation

```bash
dotnet add package Soenneker.Deduplication.Bounded
```

## What it does

`Soenneker.Deduplication.Bounded` provides a fast, thread-safe *“seen set”* for deduplication with a **maximum size**.

You call `TryMarkSeen(...)` with an input value:

* **Returns `true`** if this value has **not** been seen before (it was added)
* **Returns `false`** if it has already been seen (already exists)

Internally it hashes your input to a `ulong` using **XXH3 (XxHash3)** and stores only the hash in a bounded concurrent set. That means it’s very memory efficient and avoids storing original strings/byte arrays.

## Key characteristics

* **Bounded size**: targets `MaxSize` and opportunistically trims under contention (best-effort, not strict)
* **Thread-safe**: safe to use concurrently from many threads
* **High-throughput**: stores `ulong` hashes instead of strings
* **Span-friendly**: avoids allocations via `ReadOnlySpan<char>` and `ReadOnlySpan<byte>`
* **Optional hashing seed**: lets you rotate/partition hash space if desired
* **Diagnostics-friendly**: exposes an approximate `Count`

## Quick start

```csharp
using Soenneker.Deduplication.Bounded;

var dedupe = new BoundedDedupe(maxSize: 250_000);

// returns true the first time
if (dedupe.TryMarkSeen("user:123"))
{
    // process first occurrence
}

// returns false on repeats
if (!dedupe.TryMarkSeen("user:123"))
{
    // duplicate
}
```

## API

### TryMarkSeen

Use these for the fast “check + add” operation.

```csharp
bool added = dedupe.TryMarkSeen("some string");
bool added2 = dedupe.TryMarkSeen("some string".AsSpan());
bool added3 = dedupe.TryMarkSeenUtf8(utf8Bytes);
```

### Contains

Pure membership checks (no mutation).

```csharp
bool exists = dedupe.Contains("some string");
bool exists2 = dedupe.Contains("some string".AsSpan());
bool exists3 = dedupe.ContainsUtf8(utf8Bytes);
```

### TryRemove

Removes an entry if present.

```csharp
bool removed = dedupe.TryRemove("some string");
bool removed2 = dedupe.TryRemove("some string".AsSpan());
bool removed3 = dedupe.TryRemoveUtf8(utf8Bytes);
```

### Properties

```csharp
int max = dedupe.MaxSize;
int approx = dedupe.Count; // approximate; good for diagnostics/telemetry
```

## Configuration

```csharp
var dedupe = new BoundedDedupe(
    maxSize: 250_000,
    capacityHint: 300_000,           // optional, reduces resizing
    seed: 0,                         // optional XXH3 seed
    trimBatchSize: 64,               // work chunk size when trimming
    trimStartOveragePercent: 5,      // begin trimming after +5% over MaxSize
    maxTrimWorkPerCall: 4096,        // caps trimming effort per write
    resyncAfterNoProgress: 8,        // resync count if trimming stalls
    queueOverageFactor: 4            // internal queue sizing multiplier
);
```

### Notes on trimming / bounded behavior

This is **not a strict LRU** and does **not guarantee exact eviction order**. Under heavy contention it may temporarily exceed `MaxSize`, then trims opportunistically during subsequent writes.

This design is intentional: it favors throughput and low contention over perfect eviction accuracy.

## Hashing & collisions

Inputs are deduped by their **64-bit XXH3 hash** (`ulong`). Like all hashing-based dedupe approaches, there is a theoretical possibility of collisions (different inputs producing the same hash). For most dedupe/telemetry/rate-limit style workloads, a 64-bit hash is typically more than sufficient.

If collision risk is unacceptable for your use case, you should store full keys (or use a stronger scheme), at higher memory cost.

## When to use this

* Deduping inbound events/messages by ID for a fixed memory budget
* “Seen recently” protection in high-volume ingestion pipelines
* De-duplicating phone numbers / emails / identifiers without storing raw values
* Fast in-memory suppression lists

## When not to use this

* You need **exact** dedupe of raw strings (no collision tolerance)
* You need strict FIFO/LRU eviction ordering guarantees
* You need time-window expiration semantics (use a sliding window approach instead)