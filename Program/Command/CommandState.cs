namespace Takap.Ulitity
{
    /// <summary>
    /// コマンドの状態を表します。
    /// </summary>
    public enum CommandState
    {
        /// <summary>初期値</summary>
        Default,
        /// <summary>インスタンス生成～ランナーに投入するまでの間</summary>
        Created,
        /// <summary>ランナーに投入されて実行キューに入った</summary>
        Queued,
        /// <summary>実行中</summary>
        Executing,
        /// <summary>完了</summary>
        Completed,
        /// <summary>キャンセル中</summary>
        Canceling,
        /// <summary>キャンセル終了</summary>
        Canceled,
        /// <summary>エラー終了</summary>
        Error,
    }
}
