using System;
using System.Collections.Generic;

namespace Takap.Ulitity
{
    /// <summary>
    /// コマンドIDを表します。
    /// </summary>
    public readonly struct CommandID : IEquatable<CommandID>
    {
        /// <summary>無効なコマンドIDを表します。</summary>
        public static readonly CommandID Useless = new("");

        public readonly string Value;

        public CommandID(string value)
        {
            Value = value;
        }

        // 基本型と互換を取る(必要な時だけ実装)
        //public static implicit operator string(CommandID value) => value.Value;
        //public static implicit operator CommandID(string value) => new CommandID(value);

        // 比較演算子
        public static bool operator ==(CommandID left, CommandID right) => !(left == right);
        public static bool operator !=(CommandID left, CommandID right) => left.Equals(right);

        // IEquatable<T>の実装
        public bool Equals(CommandID other) => EqualityComparer<string>.Default.Equals(Value, other.Value);

        public override bool Equals(object? obj)
        {
            return obj is CommandID sample && Equals(sample);
        }
        public override int GetHashCode() => EqualityComparer<string>.Default.GetHashCode(Value);
        public override string ToString() => $"{{{nameof(Value)}={Value}}}";
    }
}
