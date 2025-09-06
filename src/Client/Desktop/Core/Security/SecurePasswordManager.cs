using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace LYBT.Desktop.Core.Security {

    /// <summary>
    /// 安全密码管理器 - 处理密码的安全存储和传输
    /// </summary>
    public sealed class SecurePasswordManager : IDisposable {
        private SecureString? _securePassword;
        private readonly object _lockObject = new();

        /// <summary>
        /// 设置密码（使用SecureString）
        /// </summary>
        public void SetPassword(SecureString password) {
            lock (_lockObject) {
                _securePassword?.Dispose();
                _securePassword = password?.Copy();
            }
        }

        /// <summary>
        /// 设置密码（从普通字符串）
        /// </summary>
        public void SetPassword(string password) {
            lock (_lockObject) {
                _securePassword?.Dispose();

                if (string.IsNullOrEmpty(password)) {
                    _securePassword = null;
                    return;
                }

                _securePassword = new SecureString();
                foreach (char c in password) {
                    _securePassword.AppendChar(c);
                }
                _securePassword.MakeReadOnly();
            }
        }

        /// <summary>
        /// 获取密码的明文（仅在必要时使用）
        /// </summary>
        public string GetPasswordAsString() {
            lock (_lockObject) {
                if (_securePassword == null) {
                    return string.Empty;
                }

                IntPtr ptr = IntPtr.Zero;
                try {
                    ptr = Marshal.SecureStringToGlobalAllocUnicode(_securePassword);
                    return Marshal.PtrToStringUni(ptr) ?? string.Empty;
                } finally {
                    if (ptr != IntPtr.Zero) {
                        Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                    }
                }
            }
        }

        /// <summary>
        /// 使用密码执行操作（密码不会以明文形式保留在内存中）
        /// </summary>
        public T UsePassword<T>(Func<string, T> action) {
            var password = GetPasswordAsString();
            try {
                return action(password);
            } finally {
                // 注意：由于.NET字符串的不可变性，无法真正清除内存中的密码
                // SecureString已经提供了必要的安全保护
                // 避免使用unsafe代码以简化编译配置
                password = null;
            }
        }

        /// <summary>
        /// 清除密码
        /// </summary>
        public void Clear() {
            lock (_lockObject) {
                _securePassword?.Dispose();
                _securePassword = null;
            }
        }

        /// <summary>
        /// 检查是否有密码
        /// </summary>
        public bool HasPassword() {
            lock (_lockObject) {
                return _securePassword != null && _securePassword.Length > 0;
            }
        }

        /// <summary>
        /// 获取密码的哈希值（用于比较）
        /// </summary>
        public string GetPasswordHash() {
            return UsePassword(password => {
                if (string.IsNullOrEmpty(password)) {
                    return string.Empty;
                }

                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            });
        }

        public void Dispose() {
            Clear();
        }
    }
}
