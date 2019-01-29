using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFCore.Extensions.SqlServer.UnitTests
{
    public static class GuidExtensions
    {
        private static readonly int[] byteOrder = { 15, 14, 13, 12, 11, 10, 9, 8, 6, 7, 4, 5, 0, 1, 2, 3 };

        public static Guid NextValue(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            var canIncrement = byteOrder.Any(i => ++bytes[i] != 0);
            return new Guid(canIncrement ? bytes : new byte[16]);
        }

        public static Guid NextValue(this Guid guid, int i)
        {
            if (i < 0) throw new ArgumentOutOfRangeException(nameof(i));
            for (var j = 0; j < i; j++)
                guid = guid.NextValue();
            return guid;
        }
    }

    public class SeqGuid
    {
        public Guid Value { get; }

        public SeqGuid()
        {
            Value = Guid.NewGuid();
        }

        public SeqGuid(Guid value)
        {
            Value = value;
        }

        public static SeqGuid operator ++(SeqGuid a)
        {
            return new SeqGuid(a.Value.NextValue());
        }

        public static implicit operator Guid(SeqGuid a)
        {
            return a.Value;
        }
    }
}
