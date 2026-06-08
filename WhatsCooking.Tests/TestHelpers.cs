namespace WhatsCooking.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset localNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => localNow.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

internal sealed class RecordingProgress<T> : IProgress<T>
{
    public List<T> Values { get; } = [];

    public void Report(T value) => Values.Add(value);
}
