using System.Diagnostics;
using System.Threading;

namespace Takap.Ulitity
{
    //
    // Sample Comamnds
    // - - - - - - - - - - - - - - - - - - - -

    /// <summary>
    /// サンプル用のコマンドを表します。
    /// </summary>
    public class SampleCommand1 : CommandBase
    {
        public override string GetCommandName() => "SampleCommand1";

        public override bool DeleteImmediatelyIfCompleted => true;

        public override void Execute()
        {
            var args = GetArgs<SampleArgs>();
            Trace.WriteLine($"ID={args.ID}");

            Thread.Sleep(300); // 少し時間のかかる処理

            SetResult(new SampleResult() { Code = 999 });
        }
    }
}
