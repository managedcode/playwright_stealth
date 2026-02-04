using TUnit.Core;
using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<ManagedCode.Playwright.Stealth.Tests.MaxParallelTestsForPipeline>]

namespace ManagedCode.Playwright.Stealth.Tests;

/// <summary>
/// Limits parallel test execution to reduce resource contention in CI.
/// </summary>
public sealed class MaxParallelTestsForPipeline : IParallelLimit
{
    public int Limit => 1;
    public static readonly bool Headless = true;
}
