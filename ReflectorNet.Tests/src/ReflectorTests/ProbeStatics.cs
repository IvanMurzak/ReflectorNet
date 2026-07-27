/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using Xunit;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Serialises the test classes that observe side effects through STATIC probes
    /// (<see cref="ForeignRefRegistry"/>, <see cref="TwoPhaseProbeTarget"/>,
    /// <see cref="SilentFailureProbeTarget"/>).
    /// </summary>
    /// <remarks>
    /// Today each probe happens to be touched by exactly one test class, and xunit does not run tests
    /// within a class concurrently, so there is no live race. That safety is accidental: the probes
    /// are <c>public</c> in the test assembly, so the first cross-class use - or an
    /// <c>xunit.runner.json</c> that enables in-class parallelism - would introduce one silently, and
    /// a flaky assertion about "which setters ran" is exactly the kind of noise that erodes trust in
    /// a regression suite. Naming one collection makes the constraint explicit and cheap to keep.
    /// </remarks>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class ProbeStatics
    {
        public const string Name = "probe-statics";
    }
}
