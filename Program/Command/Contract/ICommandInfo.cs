using System;
using System.Collections.Generic;

namespace Takap.Ulitity
{
    /// <summary>
    /// コマンドの情報を表します。
    /// </summary>
    public interface ICommandInfo
    {
        /// <summary>
        /// コマンド名を取得します。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// JSON 形式の引数文字列を取得します。
        /// </summary>
        string ArgsJsonString { get; }

        /// <summary>
        /// 実行結果を取得します。
        /// </summary>
        CommandResult CommandResult { get; }

        /// <summary>
        /// コマンドエラー時のエラー情報を取得します。
        /// </summary>
        Exception ErrorInfo { get; }

        /// <summary>
        /// このコマンドを識別するための識別子を取得します。
        /// </summary>
        CommandID ID { get; }

        /// <summary>
        /// タスクの優先度を取得します。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// タスクがキューにエントリーされた時間を取得します。
        /// </summary>
        DateTime EntryTime { get; }

        /// <summary>
        /// タスクが実行された時間を取得します。
        /// </summary>
        DateTime ExecuteTime { get; }

        /// <summary>
        /// タスクが終了した時間を取得します。
        /// </summary>
        DateTime CompleteTime { get; }

        /// <summary>
        /// タスクの状態を取得します。
        /// </summary>
        CommandState State { get; }

        /// <summary>
        /// コマンドの状態の履歴を取得します。
        /// </summary>
        IReadOnlyCollection<CommandStateInfo> GetHistory();
    }
}
