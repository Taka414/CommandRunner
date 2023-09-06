using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Takap.Ulitity
{
    /// <summary>
    /// コマンドを蓄積して順次実行するクラス
    /// </summary>
    public class CommandRunner : IDisposable
    {
        //
        // Fields
        // - - - - - - - - - - - - - - - - - - - -

        // コマンドリスト
        readonly List<ICommand> _commandList = new();
        // リスト操作用のロック用のオブジェクト
        readonly object _lockObj = new();
        // タスク削除用のタイマー
        readonly System.Timers.Timer _timer;
        // 削除対象のコマンドリスト
        readonly List<ICommand> _removeList = new();

        //
        // Events
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// コマンド実行が完了したときに発生します。
        /// </summary>
        public event Action<ICommand> CommandCompleted;

        //
        // Props
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 同時実行数を設定または取得します。既定値は 1です。
        /// </summary>
        public int ConcurrentCount { get; set; } = 1;

        /// <summary>
        /// 実行完了後に放置されたコマンドを削除するまでの時間を設定または取得します。
        /// </summary>
        public TimeSpan CommandHoldTime { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 現在キューに入っているコマンド数を取得します。
        /// </summary>
        public int CommandCount
        {
            get
            {
                lock (_lockObj)
                {
                    return _commandList.Count;
                }
            }
        }

        //
        // Construcotrs
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 既定の初期値でオブジェクトを初期化します。
        /// </summary>
        public CommandRunner()
        {
            _timer = new System.Timers.Timer(250); // 1秒間に4回固定
            _timer.Elapsed += OnTimerElapsed;
        }

        //
        // IDisposable impl
        // - - - - - - - - - - - - - - - - - - - -

        public void Dispose()
        {
            lock (_lockObj)
            {
                foreach (var item in _removeList)
                {
                    item.Cancel();
                }
            }

            using (_timer) { }
            GC.SuppressFinalize(this);
        }

        //
        // Public Methods
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// コマンドを実行キューに追加します。
        /// </summary>
        public CommandID Entry(CommandBase command)
        {
            Trace.WriteLine($"Entry={command}");
            command.State = CommandState.Queued;

            lock (_lockObj)
            {
                // 末尾に追加 → 優先度順に最上位コマンドを探して実行する
                _commandList.Add(command);
                CommandExecute();

                if (!_timer.Enabled)
                {
                    _timer.Start();
                    Trace.WriteLine("Timer start");
                }
            }

            return command.ID;
        }

        /// <summary>
        /// 指定した ID のコマンドをキャンセルします。
        /// </summary>
        /// <returns>
        /// true: コマンドに対してキャンセルを実行した / false: それ以外
        /// </returns>
        public bool Cancel(CommandID id)
        {
            lock (_lockObj)
            {
                if (TryGetCommandByID(id, out ICommand command))
                {
                    if (command.State == CommandState.Default ||
                        command.State == CommandState.Created ||
                        command.State == CommandState.Queued ||
                        command.State == CommandState.Executing)
                    {
                        command.Cancel();
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// コマンドを取得します。
        /// </summary>
        /// <returns>
        /// true: 情報が取得できた / false: 取得できなかった
        /// </returns>
        public bool TryGetCommandInfo(CommandID id, out ICommandInfo result)
        {
            result = null;
            lock (_lockObj)
            {
                if (TryGetCommandByID(id, out ICommand command))
                {
                    result = command.GetCommandInfo();
                }
            }
            return result != null;
        }

        /// <summary>
        /// 実行が完了したコマンドの情報を取得します。
        /// </summary>
        /// <returns>
        /// true: 情報が取得できた / false: 取得できなかった
        /// </returns>
        public bool TryGetFinishedCommandInfo(CommandID id, out ICommandInfo result)
        {
            result = null;
            lock (_lockObj)
            {
                if (TryGetCommandByID(id, out ICommand command))
                {
                    // 終了している時だけ取得できる
                    if (command.State == CommandState.Canceled ||
                        command.State == CommandState.Completed ||
                        command.State == CommandState.Error)
                    {
                        _commandList.Remove(command); // いちど結果をとったオブジェクトは削除
                        result = command.GetCommandInfo();
                        using (command) { }
                        Trace.WriteLine($"Remove(GetResult)={command}");
                    }
                }
            }
            return result != null;
        }

        /// <summary>
        /// 指定した ID のコマンドを削除します。
        /// </summary>
        /// <returns>
        /// true: 削除できた / false: それ以外
        /// </returns>
        public bool Remove(CommandID id)
        {
            lock (_lockObj)
            {
                if (TryGetCommandByID(id, out ICommand command))
                {
                    // 動作中は削除できない
                    if (!(command.State == CommandState.Executing || command.State == CommandState.Canceling))
                    {
                        _commandList.Remove(command);
                        using (command) { }
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 現在エントリー中のコマンド一覧を取得します。
        /// </summary>
        public ICommandInfo[] GetEntriedCommands()
        {
            lock (_lockObj)
            {
                ICommandInfo[] retArray = new ICommandInfo[_commandList.Count];
                for (int i = 0; i < retArray.Length; i++)
                {
                    ICommand command = _commandList[i];
                    retArray[i] = command.GetCommandInfo();
                }
                return retArray;
            }
        }

        //
        // Methods
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 現在実行中のコマンドの数を取得します。
        /// </summary>
        private int GetExecutingCommandCount()
        {
            int count = 0;
            lock (_lockObj)
            {
                foreach (var item in CollectionsMarshal.AsSpan(_commandList))
                {
                    if (item.State == CommandState.Executing)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// 現在のコマンドリストからコマンドを実行します。
        /// </summary>
        private void CommandExecute()
        {
            lock (_lockObj)
            {
                while (GetExecutingCommandCount() < ConcurrentCount)
                {
                    if (!TryGetTopCommand(out ICommand command))
                    {
                        break;
                    }
                    ((CommandBase)command).Completed += OnCommandCompleted;
                    command.ExecuteFireAndForget();
                    Trace.WriteLine($"Execute={command}");
                    // 長時間コマンドが終わらないと次のコマンドが開始されないケースが出てくるので注意すること
                }
            }
        }

        /// <summary>
        /// 次に実行するコマンドを取得します。
        /// </summary>
        private bool TryGetTopCommand(out ICommand command)
        {
            ICommand select = null;
            foreach (var item in CollectionsMarshal.AsSpan(_commandList))
            {
                if (select == null && item.State == CommandState.Queued)
                {
                    select = item;
                    continue;
                }

                // 優先度準が高くてエントリー時刻が早いものを選択する
                if (item.State == CommandState.Created && (item.Priority == select.Priority && item.EntryTime < select.EntryTime || item.Priority > select.Priority))
                {
                    select = item;
                }
            }
            command = select;
            return command != null;
        }

        /// <summary>
        /// 指定したキーに関連するコマンドを取得します。
        /// </summary>
        private bool TryGetCommandByID(CommandID id, out ICommand command)
        {
            command = null;
            foreach (var item in CollectionsMarshal.AsSpan(_commandList))
            {
                if (item.ID == id)
                {
                    command = item;
                    break;
                }
            }
            return command != null;
        }

        /// <summary>
        /// コマンドが実行完了した。
        /// </summary>
        private void OnCommandCompleted(ICommand command)
        {
            Trace.WriteLine($"Complete={command}");
            try
            {
                CommandCompleted?.Invoke(command);
            }
            catch (Exception)
            {
                // nop
            }

            lock (_lockObj)
            {
                if (command.DeleteImmediatelyIfCompleted)
                {
                    _commandList.Remove(command);
                    using (command) { }
                }

                CommandExecute();
            }
        }

        /// <summary>
        /// タスク消去用の定周期処理を行います。
        /// </summary>
        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                DateTime now = DateTime.Now;
                lock (_lockObj)
                {
                    _removeList.Clear();
                    foreach (var command in CollectionsMarshal.AsSpan(_commandList))
                    {
                        switch (command.State)
                        {
                            case CommandState.Completed:
                            case CommandState.Canceled:
                            case CommandState.Error:
                            {
                                if (command.DeleteImmediatelyIfCompleted || now - command.CompleteTime > CommandHoldTime)
                                {
                                    _removeList.Add(command);
                                }
                                break;
                            }
                        }
                    }

                    foreach (var command in CollectionsMarshal.AsSpan(_removeList))
                    {
                        _commandList.Remove(command);
                        Trace.WriteLine($"Remove(Auto)={command}");
                        using (command) { }
                    }
                }

                if (_commandList.Count == 0)
                {
                    _timer.Enabled = false; // 無駄なので蓄積がゼロならタイマーを停止
                    Trace.WriteLine("Timer stop");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
    }
}
