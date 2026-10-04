namespace InternshipManagementApi.Services
{
    public sealed record StudentDocumentFileInspection(
        bool IsValid,
        string? Error,
        string? FileName,
        string? ContentType,
        byte[]? Content)
    {
        public static StudentDocumentFileInspection Invalid(string error) => new(false, error, null, null, null);
        public static StudentDocumentFileInspection Valid(string fileName, string contentType, byte[] content) =>
            new(true, null, fileName, contentType, content);
    }

    public static class StudentDocumentFileValidator
    {
        public const long MaxFileSize = 10 * 1024 * 1024;

        public static async Task<StudentDocumentFileInspection> InspectAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return StudentDocumentFileInspection.Invalid("Vui lòng chọn tệp không rỗng.");
            if (file.Length > MaxFileSize)
                return StudentDocumentFileInspection.Invalid("Dung lượng tệp tối đa là 10 MB.");

            var safeName = Path.GetFileName(file.FileName.Replace('\\', '/'));
            safeName = new string(safeName.Where(character => !char.IsControl(character)).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 255)
                return StudentDocumentFileInspection.Invalid("Tên tệp không hợp lệ hoặc dài hơn 255 ký tự.");

            var extension = Path.GetExtension(safeName).ToLowerInvariant();
            var content = new byte[(int)file.Length];
            await using (var stream = file.OpenReadStream())
            {
                var offset = 0;
                while (offset < content.Length)
                {
                    var read = await stream.ReadAsync(content.AsMemory(offset));
                    if (read == 0) break;
                    offset += read;
                }
                if (offset != content.Length)
                    return StudentDocumentFileInspection.Invalid("Không thể đọc đầy đủ nội dung tệp.");
            }

            var isPdf = extension == ".pdf" && content.AsSpan().StartsWith("%PDF-"u8);
            var isLegacyWord = extension == ".doc" && content.AsSpan().StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
            var isDocx = extension == ".docx" && content.AsSpan().StartsWith(new byte[] { 0x50, 0x4B, 0x03, 0x04 });
            if (!isPdf && !isLegacyWord && !isDocx)
                return StudentDocumentFileInspection.Invalid("Chỉ chấp nhận tệp PDF, DOC hoặc DOCX đúng định dạng.");

            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                _ => "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };
            return StudentDocumentFileInspection.Valid(safeName, contentType, content);
        }
    }
}
