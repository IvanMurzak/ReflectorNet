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
    /// This is no longer a precaution: <see cref="ForeignRefRegistry"/> is now touched by BOTH
    /// <c>ConverterFallThroughTests</c> (the scalar consumer seam) and
    /// <c>CollectionDeserializationFailureTests</c> (the collection one), so without this collection
    /// the two would race on a process-global dictionary and on <c>LookupCount</c>. Any further test
    /// class that reads or writes one of these probes MUST join it.
    /// </remarks>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class ProbeStatics
    {
        public const string Name = "probe-statics";
    }
}
