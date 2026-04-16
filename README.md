# AutoTransfer

WPF desktop tool for screen capture, template matching, and Windows automation on `.NET 8`.

## Overview

Project này dùng để test một flow automation UI đơn giản trên Windows:

1. Chụp màn hình
2. Kéo chọn một vùng làm template
3. Tìm lại template trên ảnh chụp bằng OpenCV
4. Click vào vị trí tìm được
5. Gửi text thử bằng bàn phím ảo

App phù hợp để kiểm tra nhanh các khối nền tảng trước khi ghép vào bot hoặc tool automation lớn hơn.

## Features

- Chụp toàn màn hình và preview trực tiếp trong app
- Crop template từ ảnh chụp bằng cửa sổ chọn vùng
- Template matching bằng `OpenCvSharp`
- Click chuột tại điểm tìm được
- Gửi text thử bằng keyboard automation
- Hỗ trợ DPI-aware theo `logical pixels` để chạy đúng trên các mức scale Windows như `125%`, `150%`, `175%`

## Tech Stack

- `WPF` cho giao diện desktop
- `.NET 8` cho runtime chính
- `OpenCvSharp4` cho template matching
- `KAutoHelper_Kteam` cho screen capture
- `InputSimulatorPlus` cho mouse/keyboard automation
- `System.Drawing.Common` cho bitmap crop/resize

## Quick Start

### Requirements

- Windows
- Visual Studio 2022+ hoặc `.NET 8 SDK`
- Workload `.NET desktop development` nếu build bằng Visual Studio

### Open

- Mở `AutoTransfer.sln` trong Visual Studio

### Build

```powershell
dotnet build
```

### Run

```powershell
dotnet run --project AutoTransfer.csproj
```

## Test Flow

Trong giao diện chính, flow test cơ bản là:

1. `Chụp màn hình`
2. `Chọn vùng template`
3. `Tìm ảnh mẫu`
4. `Click điểm tìm được`
5. `Gửi text thử`

## Project Structure

- `App.xaml`: entry point WPF
- `MainWindow.xaml`: giao diện chính
- `MainWindow.xaml.cs`: luồng capture, crop, find, click, keyboard test
- `TemplateCropWindow.xaml`: UI chọn vùng template
- `TemplateCropWindow.xaml.cs`: xử lý crop bằng chuột
- `ScreenScaling.cs`: normalize `physical -> logical` khi capture và `logical -> physical` khi click
- `AutomationTools.cs`: wrapper cho click chuột và nhập text
- `app.manifest`: cấu hình DPI awareness cho Windows

## DPI Handling

App xử lý capture, crop và template matching theo `logical pixels` thay vì `physical pixels`.

Điều này giúp:

- vùng người dùng nhìn thấy trên desktop
- vùng crop lưu ra
- vùng match tìm được

khớp nhau ngay cả khi màn hình đang để scale khác `100%`.

Chỉ bước click chuột mới đổi ngược sang `physical pixels`.

Chi tiết xem tại [LOGICAL_DPI_FIX.md](./LOGICAL_DPI_FIX.md).

## Notes

- Một số package hiện vẫn là package cũ restore theo `.NET Framework`, nên `dotnet build` có warning `NU1701`
- Warning này hiện không chặn build hay chạy app, nhưng nếu muốn production hóa thì nên thay bằng package tương thích `.NET 8`
