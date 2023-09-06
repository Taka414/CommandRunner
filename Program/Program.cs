using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace Takap.Ulitity
{
    internal class Program
    {
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        private static void Main(string[] args)
        {
            // コマンド実行用のオブジェクトの作成
            CommandRunner _runner = new();
            try
            {
                // コマンドが終了したときのコールバックを設定
                _runner.CommandCompleted += c =>
                {
                    Trace.WriteLine(c.ToString());
                    foreach (var info in c.GetCommandInfo().GetHistory())
                    {
                        Trace.WriteLine($"[{GetCurrentThreadId():D6}] {info}");
                    }
                };

                // 同時実行数を2に設定
                _runner.ConcurrentCount = 2;

                // サンプルコマンドを作成して順次登録
                for (int i = 0; i < 100; i++)
                {
                    // 少し時間のかかる処理
                    var command1 = new SampleCommand1()
                    {
                        ArgsJsonString = JsonSerializer.Serialize(new SampleArgs() { ID = i })
                    };

                    // コマンドを実行キューに登録
                    // コマンドキューには最大容量が無いので詰み過ぎがある場合制限を追加で実装する
                    _runner.Entry(command1);
                }

                Thread.Sleep(-1);
            }
            finally
            {
                // 使い終わったら破棄する
                using (_runner) { }
            }
        }
    }
}
