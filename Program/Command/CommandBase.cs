using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace Takap.Ulitity
{
    /// <summary>
    /// コマンドを実装するクラスが継承すべき基底クラスを表します。
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        // 
        // Const
        // - - - - - - - - - - - - - - - - - - - -

        static readonly Type ArgsInterfaceType = typeof(ICommandArgs);

        // 
        // Fields
        // - - - - - - - - - - - - - - - - - - - -

        // スレッド実行用のタスク
        Task _task;

        // コマンドの情報のコンテナ
        readonly CommandInfo _commandInfo = CommandInfo.GetCommandInfo();

        /// <summary>
        /// JSON 形式の引数文字列を取得します。
        /// </summary>
        public string ArgsJsonString { get => _commandInfo.ArgsJsonString; set => _commandInfo.ArgsJsonString = value; }

        /// <summary>
        /// このコマンドを識別するための識別子を取得します。
        /// </summary>
        public CommandID ID => _commandInfo.ID;

        /// <summary>
        /// タスクの優先度を取得します。
        /// </summary>
        public int Priority => _commandInfo.Priority;

        /// <summary>
        /// タスクの状態を取得します。
        /// </summary>
        public CommandState State { get => _commandInfo.State; internal set => _commandInfo.State = value; }

        // 
        // Constructors
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 既定の初期値でオブジェクトを初期化します。
        /// </summary>
        public CommandBase()
        {
            _commandInfo.Name = GetCommandName();
        }

        /// <summary>
        /// 優先度を指定してオブジェクトを初期化します。
        /// </summary>
        public CommandBase(int priority) : this()
        {
            _commandInfo.Priority = priority;
        }

        /// <summary>
        /// オブジェクトを破棄します。
        /// </summary>
        ~CommandBase()
        {
            Dispose();
        }

        // 
        // ICommand impl
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// コマンド実行が完了したときに発生します。
        /// </summary>
        internal Action<ICommand> Completed { get; set; }

        /// <summary>
        /// タスクがキューにエントリーされた時間を取得します。
        /// </summary>
        public DateTime EntryTime => _commandInfo.EntryTime;

        /// <summary>
        /// タスクが終了した時間を取得します。
        /// </summary>
        public DateTime CompleteTime => _commandInfo.CompleteTime;

        /// <summary>
        /// キャンセルを受け付けたかどうかを取得します。
        /// true: 受け付けた / false: それ以外
        /// </summary>
        /// <remarks>
        /// 実際にキャンセルされたかどうかではありません。
        /// </remarks>
        public bool IsCancel { get; private set; }

        /// <summary>
        /// タスクを実行します。
        /// </summary>
        public void ExecuteFireAndForget()
        {
            _commandInfo.State = CommandState.Executing;
            _task = Task.Run(()=>
            {
                Trace.WriteLine($"Execute: {_commandInfo.ID}");

                try
                {
                    if (IsCancel)
                    {
                        throw new OperationCanceledException("Canceled."); // キューイング中にキャンセルを受け付けていた
                    }

                    Execute();
                    _commandInfo.State = CommandState.Completed;
                }
                catch (OperationCanceledException)
                {
                    _commandInfo.State = CommandState.Canceled;
                }
                catch (Exception ex)
                {
                    _commandInfo.State = CommandState.Error;
                    _commandInfo.ErrorInfo = ex;
                }
                finally
                {
                    Completed?.Invoke(this);
                }
            });
        }

        /// <summary>
        /// キャンセルを実行します。
        /// </summary>
        public void Cancel()
        {
            IsCancel = true; // フラグ立てるだけ → 実際の終了はコマンド
            _commandInfo.State = CommandState.Canceling; // キャンセル中にはするけど Canceled になるかは実装次第
        }

        /// <summary>
        /// コマンドの情報を取得します。
        /// </summary>
        public ICommandInfo GetCommandInfo() => _commandInfo.Clone();

        // 
        // IDisposable impl
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 使用しているリソースを解放してオブジェクトの利用を終了します。
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                Completed = null;
                GC.SuppressFinalize(this);
            }
            catch (Exception)
            {
                // nop
            }
        }

        /// <summary>
        /// 派生クラス側でリソース破棄が必要な場合このメソッドを実装します。
        /// </summary>
        protected virtual void Dipose(bool dispose)
        {
            Dispose();
        }

        // 
        // Methods
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// このオブジェクトを文字列として取得します。
        /// </summary>
        public override string ToString() => JsonSerializer.Serialize(_commandInfo, new JsonSerializerOptions() { IncludeFields = true });

        // 
        // Abstract
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// このコマンドの名称を取得します。
        /// </summary>
        public abstract string GetCommandName();

        /// <summary>
        /// コマンドを実行します。
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// コマンドが終了したときにコマンドを即時削除するかどうかを表します。
        /// true: 終了時に即座に削除 / false: 操作またはスケジューラーに従ってしばらく保持する
        /// </summary>
        public virtual bool DeleteImmediatelyIfCompleted { get; } = false;

        // 
        // Methods
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// キャンセルを受け付けたか確認して受け付けていた場合例外を throw します。
        /// </summary>
        /// <exception cref="OperationCanceledException">キャンセルされた。</exception>
        protected void CheckCancelIfThrowException()
        {
            if (IsCancel) throw new OperationCanceledException();
        }

        /// <summary>
        /// 実行引数を取得します。
        /// </summary>
        protected T GetArgs<T>()
        {
            if (string.IsNullOrEmpty(_commandInfo.ArgsJsonString))
            {
                return default;
            }

            bool isImplemented = ArgsInterfaceType.IsAssignableFrom(typeof(T));
            if (!isImplemented)
            {
                throw new NotSupportedException($"Classes that do not inherit {nameof(ICommandArgs)} cannot be used as arguments.");
            }

            return JsonSerializer.Deserialize<T>(_commandInfo.ArgsJsonString);
        }

        /// <summary>
        /// 実行結果を設定します。
        /// </summary>
        protected void SetResult(CommandResult result) => _commandInfo.CommandResult = result;

        /// <summary>
        /// コマンドの状態の履歴を取得します。
        /// </summary>
        public IReadOnlyCollection<CommandStateInfo> GetHistory() => _commandInfo.GetHistory();
    }
}
