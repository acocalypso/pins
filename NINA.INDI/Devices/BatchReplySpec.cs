#region "copyright"

/*
    Copyright © 2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Linq;
using System.Text;

namespace NINA.INDI.Devices {

    /// <summary>
    /// Describes how the replies to a batch of raw LX200 commands are terminated.
    /// <para>
    /// Most commands answer with text up to a '#', but a few answer with a single character and no
    /// terminator at all, for example the 10micron commands :Guaf#, :Gdat# and :GREF#. Reading such a
    /// batch by counting '#' alone would wait for a terminator that never arrives, so the caller says
    /// per command what to expect by appending a mask to the batch: "&lt;commands&gt;|&lt;mask&gt;",
    /// one character per reply, <see cref="SectionReply"/> or <see cref="CharacterReply"/>.
    /// </para>
    /// <para>
    /// A batch without a mask keeps the original meaning: one '#'-terminated reply per command.
    /// </para>
    /// </summary>
    public static class BatchReplySpec {

        /// <summary>Reply runs up to and including the next '#'.</summary>
        public const char SectionReply = 's';

        /// <summary>Reply is a single character with no terminator.</summary>
        public const char CharacterReply = 'c';

        public const char MaskSeparator = '|';

        /// <summary>
        /// Splits "commands|mask" into the commands to send and the mask to read them back with.
        /// Without a separator the mask is derived from the commands, one section per '#'.
        /// </summary>
        public static void Parse(string parameters, out string commands, out string mask) {
            if (parameters == null) {
                throw new ArgumentNullException(nameof(parameters));
            }

            var separatorIndex = parameters.IndexOf(MaskSeparator);
            if (separatorIndex < 0) {
                commands = parameters;
                mask = new string(SectionReply, parameters.Count(c => c == '#'));
                return;
            }

            commands = parameters.Substring(0, separatorIndex);
            mask = parameters.Substring(separatorIndex + 1);
            if (mask.Length != commands.Count(c => c == '#')) {
                throw new ArgumentException($"Reply mask '{mask}' does not describe the {commands.Count(c => c == '#')} command(s) in '{commands}'", nameof(parameters));
            }
            var unknown = mask.FirstOrDefault(c => c != SectionReply && c != CharacterReply);
            if (unknown != default(char)) {
                throw new ArgumentException($"Reply mask '{mask}' contains '{unknown}', expected only '{SectionReply}' or '{CharacterReply}'", nameof(parameters));
            }
        }

        /// <summary>
        /// Reads one reply per mask entry and returns them concatenated, exactly as they arrived.
        /// <paramref name="readByte"/> returns -1 at end of stream, like <see cref="System.IO.Stream.ReadByte"/>.
        /// </summary>
        public static string Read(Func<int> readByte, string mask) {
            if (readByte == null) {
                throw new ArgumentNullException(nameof(readByte));
            }
            if (mask == null) {
                throw new ArgumentNullException(nameof(mask));
            }

            var replies = new StringBuilder();
            for (int i = 0; i < mask.Length; i++) {
                var readToTerminator = mask[i] == SectionReply;
                while (true) {
                    var b = readByte();
                    // A partial batch must not be returned: the caller matches replies to commands by position.
                    if (b < 0) {
                        throw new System.IO.IOException($"Connection closed after {i} of {mask.Length} batch replies");
                    }
                    var c = (char)b;
                    replies.Append(c);
                    if (!readToTerminator || c == '#') {
                        break;
                    }
                }
            }
            return replies.ToString();
        }
    }
}
