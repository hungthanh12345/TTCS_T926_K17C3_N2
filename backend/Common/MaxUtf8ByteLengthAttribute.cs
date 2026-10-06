using System.ComponentModel.DataAnnotations;
using System.Text;

namespace InternshipManagementApi.Common
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class MaxUtf8ByteLengthAttribute : ValidationAttribute
    {
        public MaxUtf8ByteLengthAttribute(int maximumBytes)
        {
            if (maximumBytes < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumBytes));

            MaximumBytes = maximumBytes;
        }

        public int MaximumBytes { get; }

        public override bool IsValid(object? value) =>
            value is null || (value is string text && Encoding.UTF8.GetByteCount(text) <= MaximumBytes);

        public override string FormatErrorMessage(string name) =>
            $"{name} cannot exceed {MaximumBytes} UTF-8 bytes.";
    }
}
