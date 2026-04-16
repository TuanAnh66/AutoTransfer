# AutoTransfer

Desktop app WPF C# (.NET 8).

## Open
- Mở `AutoTransfer.sln` trong Visual Studio 2022+

## Build
- Cần cài .NET 8 SDK hoặc Visual Studio có workload `.NET desktop development`

## Structure
- `App.xaml` - entry point WPF
- `MainWindow.xaml` - cửa sổ chính
- `MainWindow.xaml.cs` - code-behind
- `AutoTransfer.csproj` - project file

## DPI Note
- App xử lý capture, crop và template matching theo `logical pixels` để chạy đúng trên mọi mức scale Windows như `125%`, `150%`, `175%`
- Chỉ bước click chuột mới đổi ngược từ `logical` sang `physical pixels`
- Tóm tắt chi tiết xem tại `LOGICAL_DPI_FIX.md`
