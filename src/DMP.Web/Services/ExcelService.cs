using OfficeOpenXml;

namespace DMP.Web.Services;

public interface IExcelService
{
    List<string> ReadPhoneNumbers(IFormFile file);
}

public class ExcelService : IExcelService
{
    public List<string> ReadPhoneNumbers(IFormFile file)
    {
        var phoneNumbers = new List<string>();

        using var stream = new MemoryStream();
        file.CopyTo(stream);
        stream.Position = 0;

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return phoneNumbers;

        for (int row = 1; row <= worksheet.Dimension.End.Row; row++)
        {
            var value = worksheet.Cells[row, 1].Text?.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                // Remove any spaces, dashes, or special characters
                var cleaned = new string(value.Where(c => char.IsDigit(c) || c == '+').ToArray());
                if (!string.IsNullOrEmpty(cleaned))
                    phoneNumbers.Add(cleaned);
            }
        }

        return phoneNumbers;
    }
}
