# CommandRunner

汎用コマンドバッファリングと実行の基本処理 / Basic process for generic command buffering and execution


### 使い方 / how to use

使い方は以下の通りです / The usage is as follows

```cs
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace Takap.Ulitity
{
    internal class AppMain
    {
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        private static void Main(string[] args)
        {
            // コマンド実行用のオブジェクトの作成
            // Create object for command execution
            CommandRunner _runner = new();
            try
            {
                // コマンドが終了したときのコールバックを設定
                // Set callback when command terminates
                _runner.CommandCompleted += c =>
                {
                    Trace.WriteLine(c.ToString());
                    foreach (var info in c.GetCommandInfo().GetHistory())
                    {
                        Trace.WriteLine($"[{GetCurrentThreadId():D6}] {info}");
                    }
                };

                // 同時実行数を2に設定
                // Set concurrency to 2
                _runner.ConcurrentCount = 2;

                // サンプルコマンドを作成して順次登録
                // Create sample commands and register them sequentially
                for (int i = 0; i < 100; i++)
                {
                    // 少し時間のかかる処理
                    // Slightly time-consuming process
                    var command1 = new SampleCommand1()
                    {
                        ArgsJsonString = JsonSerializer.Serialize(new SampleArgs() { ID = i })
                    };

                    // コマンドを実行キューに登録
                    // register the command in the execution queue
                    // 
                    // コマンドキューには最大容量が無いので詰み過ぎがある場合制限を追加で実装する
                    // Command queue does not have a maximum capacity, 
                    // so if it is overstuffed, implement an additional limit
                    _runner.Entry(command1);
                }

                Thread.Sleep(-1);
            }
            finally
            {
                // 使い終わったら破棄する
                // Discard when finished using.
                using (_runner) { }
            }
        }
    }
}
```