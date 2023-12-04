﻿using System.Text;

namespace Prowl.Runtime.Serialization
{
    public class ByteArrayTag : Tag
	{
		public byte[] Value { get; set; }
		
		public byte this[int index]
		{
			get { return Value[index]; }
			set { Value[index] = value; }
		}

		public ByteArrayTag() : this(new byte[] { }){ }
		public ByteArrayTag(byte[] value)
		{
			value ??= new byte[] { };
            Value = (byte[])value.Clone();
        }

        public static explicit operator byte[](ByteArrayTag tag) => tag.Value;

        public override TagType GetTagType() => TagType.ByteArray;

        public override Tag Clone() => new ByteArrayTag((byte[])Value.Clone());

        public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append("ByteArrayTAG");
			sb.AppendFormat(": [{0} bytes]", Value.Length);
			return sb.ToString();
		}
	}
}