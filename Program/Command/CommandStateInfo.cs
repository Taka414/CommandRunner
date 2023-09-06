using System;

namespace Takap.Ulitity
{
    // コマンド履歴記録用の型
    public readonly struct CommandStateInfo
    {
        public readonly CommandState State;
        public readonly DateTime Time;
        public CommandStateInfo(CommandState state, DateTime time)
        {
            State = state;
            Time = time;
        }
        public override string ToString()
        {
            return $"{{ \"State\": \"{State}\", \"{nameof(Time)}\": \"{Time:yyyy/MM/dd HH:mm:ss.fff}\" }}";
        }
    }
}
