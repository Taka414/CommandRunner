using System.Text.Json;

namespace Takap.Ulitity
{
    /// <summary>
    /// コマンド実行結果であることを表すマーカーインターフェース
    /// </summary>
    public abstract class CommandResult
    {
        /// <summary>
        /// オブジェクトをJSONに変換します。
        /// </summary>
        public string ToJson() => JsonSerializer.Serialize(this, GetType());
    }
}
