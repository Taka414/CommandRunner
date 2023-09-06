using System;
using System.Collections.Generic;

namespace Takap.Ulitity
{
    /// <summary>
    /// コマンドの操作を表します。
    /// </summary>
    public interface ICommand : IDisposable
    {
        // 
        // Props
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// このコマンドを識別するための識別子を取得します。
        /// </summary>
        CommandID ID { get; }

        /// <summary>
        /// タスクの優先度を取得します。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// タスクの状態を取得します。
        /// </summary>
        CommandState State { get; }

        /// <summary>
        /// タスクがキューにエントリーされた時間を取得します。
        /// </summary>
        DateTime EntryTime { get; }

        /// <summary>
        /// タスクが終了した時間を取得します。
        /// </summary>
        DateTime CompleteTime { get; }

        /// <summary>
        /// キャンセルを受け付けたかどうかを取得します。
        /// true: 受け付けた / false: それ以外
        /// </summary>
        bool IsCancel { get; }

        /// <summary>
        /// コマンドが終了したときにコマンドを即時削除するかどうかを表します。
        /// true: 終了時に即座に削除(エラーの時も情報破棄) / false: 操作またはスケジューラーに従ってしばらく保持する
        /// </summary>
        /// <remarks>
        /// 本当に投げっぱなしにして終わったら即削除
        /// </remarks>
        bool DeleteImmediatelyIfCompleted { get; }

        // 
        // Methods
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// タスクを実行します。
        /// </summary>
        /// <remarks>
        /// Fire & Forget 方式でコマンドを実行します。
        /// </remarks>
        void ExecuteFireAndForget();

        /// <summary>
        /// キャンセルを実行します。
        /// </summary>
        void Cancel();

        /// <summary>
        /// コマンドの情報を取得します。
        /// </summary>
        ICommandInfo GetCommandInfo();

        /// <summary>
        /// コマンドの状態の履歴を取得します。
        /// </summary>
        IReadOnlyCollection<CommandStateInfo> GetHistory();
    }
}
