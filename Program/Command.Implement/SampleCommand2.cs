using System.Diagnostics;
using System.Threading;

namespace Takap.Ulitity
{
    /// <summary>
    /// サンプル用のコマンドを表します。
    /// </summary>
    public class SampleCommand2 : CommandBase
    {
        public override string GetCommandName() => "SampleCommand2";

        public override void Execute()
        {
            var args = GetArgs<SampleArgs>();
            Trace.WriteLine($"ID={args.ID}");

            Thread.Sleep(1000);

            throw new CommandException() { Code = 888, };
        }
    }
}
