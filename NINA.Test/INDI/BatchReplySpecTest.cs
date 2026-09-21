#region "copyright"

/*
    Copyright © 2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Devices;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace NINA.Test.INDI {

    [TestFixture]
    public class BatchReplySpecTest {

        // Reads the stream one byte at a time, like NetworkStream.ReadByte, and reports -1 at the end.
        private static Func<int> Reader(string stream) {
            var bytes = Encoding.ASCII.GetBytes(stream);
            var position = 0;
            return () => position < bytes.Length ? bytes[position++] : -1;
        }

        [Test]
        public void Parse_WithoutMask_ExpectsOneTerminatedReplyPerCommand() {
            BatchReplySpec.Parse(":GRTMP#:GRPRS#", out string commands, out string mask);

            Assert.That(commands, Is.EqualTo(":GRTMP#:GRPRS#"));
            Assert.That(mask, Is.EqualTo("ss"));
        }

        [Test]
        public void Parse_WithMask_SplitsCommandsFromMask() {
            BatchReplySpec.Parse(":Guaf#:GT#|cs", out string commands, out string mask);

            Assert.That(commands, Is.EqualTo(":Guaf#:GT#"));
            Assert.That(mask, Is.EqualTo("cs"));
        }

        [Test]
        public void Parse_MaskDoesNotCoverEveryCommand_Throws() {
            Assert.That(() => BatchReplySpec.Parse(":Guaf#:GT#|c", out _, out _), Throws.ArgumentException);
        }

        [Test]
        public void Parse_MaskWithUnknownCharacter_Throws() {
            Assert.That(() => BatchReplySpec.Parse(":Guaf#:GT#|cx", out _, out _), Throws.ArgumentException);
        }

        [Test]
        public void Read_TerminatedReplies_KeepsThemWithTheirTerminator() {
            var replies = BatchReplySpec.Read(Reader("+017.0#1014.9#"), "ss");

            Assert.That(replies, Is.EqualTo("+017.0#1014.9#"));
        }

        // ':Guaf#', ':Gdat#' and ':GREF#' answer with a single character and no '#'. Counting '#' alone
        // would wait for a terminator that never comes, which is what the mask is for.
        [Test]
        public void Read_SingleCharacterReplyBetweenTerminatedOnes_ReadsExactlyOneCharacter() {
            var replies = BatchReplySpec.Read(Reader("0" + "60.2#" + "1" + "5#"), "cscs");

            Assert.That(replies, Is.EqualTo("060.2#15#"));
        }

        [Test]
        public void Read_StopsAfterTheLastReply_AndLeavesTheRestOnTheStream() {
            var reader = Reader("0" + "60.2#" + "unread");

            var replies = BatchReplySpec.Read(reader, "cs");

            Assert.That(replies, Is.EqualTo("060.2#"));
            Assert.That(reader(), Is.EqualTo((int)'u'));
        }

        [Test]
        public void Read_ConnectionClosesMidBatch_ThrowsRatherThanReturningAPartialBatch() {
            Assert.That(() => BatchReplySpec.Read(Reader("60.2#101"), "sss"), Throws.TypeOf<IOException>());
        }

        [Test]
        public void Read_EmptyMask_ReadsNothing() {
            var reader = Reader("60.2#");

            Assert.That(BatchReplySpec.Read(reader, string.Empty), Is.Empty);
            Assert.That(reader(), Is.EqualTo((int)'6'));
        }
    }
}
