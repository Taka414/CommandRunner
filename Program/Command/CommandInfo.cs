using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Takap.Ulitity
{
    /// <summary>
    /// コマンドの情報を表します。
    /// </summary>
    public class CommandInfo : ICommandInfo
    {
        // 
        // Fields
        // - - - - - - - - - - - - - - - - - - - -

        // タスクの現在の状態
        private CommandState _state;
        // コマンドの状態の履歴のリスト
        private readonly List<CommandStateInfo> _stateHistoryList = new();

        // 
        // Constructors
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 既定の初期値でオブジェクトを初期化します。
        /// </summary>
        private CommandInfo() { }

        // 
        // Props
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// コマンド名を取得します。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// JSON 形式の引数文字列を取得します。
        /// </summary>
        public string ArgsJsonString { get; set; } = "";

        /// <summary>
        /// 実行結果を取得します。
        /// </summary>
        public CommandResult CommandResult { get; set; }

        /// <summary>
        /// コマンドエラー時のエラー情報を取得します。
        /// </summary>
        public Exception ErrorInfo { get; set; }

        /// <summary>
        /// このコマンドを識別するための識別子を取得します。
        /// </summary>
        public CommandID ID { get; set; } = CommandID.Useless;

        /// <summary>
        /// タスクの優先度を取得します。
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// タスクがキューにエントリーされた時間を取得します。
        /// </summary>
        public DateTime EntryTime { get; private set; }

        /// <summary>
        /// タスクが実行された時間を取得します。
        /// </summary>
        public DateTime ExecuteTime { get; private set; }

        /// <summary>
        /// タスクが終了した時間を取得します。
        /// </summary>
        public DateTime CompleteTime { get; private set; }

        /// <summary>
        /// タスクの状態を取得します。
        /// </summary>
        public CommandState State
        {
            get => _state;
            set
            {
                var now = DateTime.Now;
                switch (value)
                {
                    case CommandState.Queued:
                        EntryTime = now;
                        break;
                    case CommandState.Executing:
                        ExecuteTime = now;
                        break;
                    case CommandState.Completed:
                    case CommandState.Canceled:
                    case CommandState.Error:
                        CompleteTime = now;
                        break;
                    case CommandState.Created: break;
                    case CommandState.Canceling: break;
                    default:
                        break;
                }
                Trace.WriteLine($"[{_state}]->[{value}], {Name}@{ID}");
                _state = value;
                _stateHistoryList.Add(new CommandStateInfo(value, now));
            }
        }

        // 
        // Methods
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 新規オブジェクトを取得します。
        /// </summary>
        public static CommandInfo GetCommandInfo()
        {
            return new CommandInfo()
            {
                ID = new CommandID(Guid.NewGuid().ToString()),
                State = CommandState.Created,
            };
        }

        /// <summary>
        /// オブジェクトを複製します。
        /// </summary>
        public CommandInfo Clone()
        {
            var info = new CommandInfo
            {
                ArgsJsonString = ArgsJsonString,
                CommandResult = CommandResult,
                ErrorInfo = ErrorInfo,
                ID = ID,
                Priority = Priority,
                _state = State,
            };
            foreach (var item in CollectionsMarshal.AsSpan(_stateHistoryList))
            {
                info._stateHistoryList.Add(item);
            }
            return info;
        }

        /// <summary>
        /// コマンドの状態の履歴を取得します。
        /// </summary>
        public IReadOnlyCollection<CommandStateInfo> GetHistory()
        {
            return new ReadOnlyCollection<CommandStateInfo>(_stateHistoryList);
        }
    }
}
