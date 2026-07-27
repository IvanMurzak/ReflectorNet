/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * SPDX-License-Identifier: Apache-2.0
 * Copyright (c) 2024-2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

namespace com.IvanMurzak.ReflectorNet.Converter
{
    /// <summary>
    /// The outcome of a single converter's attempt to deserialize a <c>value</c> payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why three states and not two.</b> A converter used to answer with a single boolean, and
    /// that boolean had to carry two unrelated meanings at once:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"this payload is not my shape - another link in the chain may understand it", and</description></item>
    ///   <item><description>"this payload IS my shape, but it is broken".</description></item>
    /// </list>
    /// <para>
    /// Collapsing those two into one signal is what produced both historic defects in this area.
    /// Reporting both as SUCCESS gave the caller a fabricated <c>default(T)</c> that is
    /// indistinguishable from a real value (commit <c>12f99093</c> fixed that). Reporting both as
    /// FAILURE then severed every consumer converter that layers its own resolution on top of the
    /// base one - <c>base.TryDeserializeValueInternal(...)</c> followed by "if the base declined,
    /// resolve it my way" - which is exactly how Unity-MCP resolves an object reference such as
    /// <c>{"instanceID":"12345"}</c>. Eleven Unity tests went red.
    /// </para>
    ///
    /// <para>
    /// <b>⚠ <see cref="NotApplicable"/> must NEVER be logged at Error severity.</b> It is an ordinary,
    /// expected condition on the way to a successful resolution by the next link. It is logged at
    /// Trace. Error is reserved for the END of the chain - when nobody understood the payload -
    /// because only then is anything actually wrong. This is not a cosmetic rule: the regression
    /// above was detected as "Unhandled log message: '[Error] ...'" in a consumer whose test harness
    /// fails a test on any unexpected error log. Do not "simplify" a
    /// <see cref="NotApplicable"/> path back into an Error log or an exception.
    /// </para>
    ///
    /// <para>
    /// <b>Chain semantics.</b> The first <see cref="Handled"/> wins. The first <see cref="Failed"/>
    /// aborts the chain and propagates. If every link answers <see cref="NotApplicable"/>, the chain
    /// END raises a single, explicit <see cref="DeserializationException"/>: loudness belongs at the
    /// end, never at every link.
    /// </para>
    /// </remarks>
    public enum DeserializationOutcome
    {
        /// <summary>
        /// The converter understood the payload and produced a value.
        /// </summary>
        Handled = 0,

        /// <summary>
        /// The payload is not this converter's shape; another converter may still handle it.
        /// <b>This is NOT an error</b> - see the remarks on <see cref="DeserializationOutcome"/>.
        /// It is logged at Trace, never at Error, and never raises an exception on its own.
        /// </summary>
        NotApplicable = 1,

        /// <summary>
        /// The payload IS this converter's shape but is invalid. This is an error: it is logged at
        /// Error severity and aborts the chain.
        /// </summary>
        Failed = 2
    }
}
