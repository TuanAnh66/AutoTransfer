# Fix Crop Theo Logical Pixels Trên Windows Scale

## Vấn đề

App ban đầu crop và match theo `physical pixels` của ảnh chụp màn hình.

Ví dụ:

- Màn hình thật có ảnh chụp `3840x2160`
- Windows scale `150%`
- Kích thước người dùng nhìn thấy theo logical chỉ còn khoảng `2560x1440`

Kết quả:

- Người dùng kéo một vùng nhìn rất lớn trên màn hình
- Nhưng file `template_crop.png` lại có kích thước theo `physical pixels`
- Nên cảm giác ảnh crop bị "nhỏ", dù thực ra crop không sai

## Triệu chứng đã gặp

- Ở cửa sổ crop, vùng chọn nhìn đúng
- Log crop đúng tọa độ và kích thước
- Nhưng ảnh crop lưu ra không đúng với kích thước người dùng kỳ vọng theo mắt nhìn trên desktop

Nguyên nhân gốc là khác hệ tọa độ:

- UI đang là `logical pixels`
- Ảnh chụp và crop đang là `physical pixels`

## Cách sửa đúng

Chuyển toàn bộ flow sang hệ `logical pixels`.

### 1. Normalize ảnh chụp về logical

Tạo file [ScreenScaling.cs](./ScreenScaling.cs):

- Lấy DPI hiện tại của cửa sổ bằng `VisualTreeHelper.GetDpi(window)`
- Tính `ScaleX`, `ScaleY`
- Chụp màn hình bằng `KAutoHelper.CaptureHelper.CaptureScreen()`
- Resize ảnh physical về logical:
  - `logicalWidth = physicalWidth / scaleX`
  - `logicalHeight = physicalHeight / scaleY`
- Set resolution ảnh về `96 DPI`

Như vậy ảnh dùng trong app sẽ cùng hệ với phần người dùng nhìn thấy trên desktop.

### 2. Crop trên ảnh logical

`TemplateCropWindow` nhận `_lastCapture`, mà `_lastCapture` bây giờ đã là ảnh logical.

Nên:

- Vùng crop người dùng kéo
- Kích thước vùng crop
- File `template_crop.png`

đều khớp với kích thước nhìn thấy trên màn hình Windows sau khi scale.

### 3. Find template trên ảnh logical

`TemplateMatcher.FindTemplate(...)` giữ nguyên logic, nhưng input screenshot đã là logical.

Điều này làm cho:

- template logical
- screenshot logical

nằm cùng một hệ tọa độ, nên match ổn định hơn.

### 4. Click đổi ngược từ logical sang physical

Windows `SetCursorPos(...)` cần tọa độ physical.

Vì vậy khi đã tìm được điểm trong ảnh logical:

- `physicalX = logicalX * scaleX`
- `physicalY = logicalY * scaleY`

Phần này được xử lý qua:

- `ScreenScale.LogicalToPhysical(...)`
- `AutomationTools.ClickAt(...)`

## File đã thay đổi

- [ScreenScaling.cs](./ScreenScaling.cs)
- [MainWindow.xaml.cs](./MainWindow.xaml.cs)
- [TemplateCropWindow.xaml.cs](./TemplateCropWindow.xaml.cs)
- [MainWindow.xaml](./MainWindow.xaml)
- [AutoTransfer.csproj](./AutoTransfer.csproj)
- [app.manifest](./app.manifest)

## Các điểm quan trọng

### `ScreenScaling.cs`

Chứa:

- `ScreenScale`
- `ScreenCaptureService`
- `CaptureResult`

Vai trò:

- đọc DPI hiện tại
- convert physical -> logical khi capture
- convert logical -> physical khi click

### `MainWindow.xaml.cs`

Đã đổi từ:

- chụp trực tiếp `KAutoHelper.CaptureHelper.CaptureScreen()`

sang:

- `CaptureLatestScreen()`
- `ScreenCaptureService.CaptureLogical(this)`

Đồng thời lưu `_lastCaptureScale` để dùng lại lúc click.

### `TemplateCropWindow.xaml.cs`

Đã bỏ cách tự tính lại tọa độ phức tạp theo `Viewbox`.

Thay vào đó:

- lấy trực tiếp `e.GetPosition(ImageCanvas)`

Vì canvas đã nằm đúng trên ảnh logical đang hiển thị.

### `MainWindow.xaml`

Đã sửa preview ảnh để fit toàn bộ ảnh crop vào khung bằng `Stretch="Uniform"`.

Việc này tránh hiểu nhầm kiểu:

- crop đúng rồi
- nhưng panel preview chỉ đang hiện góc trên trái của ảnh

### `app.manifest`

Đã thêm:

- `dpiAware = true/pm`
- `dpiAwareness = PerMonitorV2`

Để app chạy DPI-aware ổn định hơn trên Windows.

## Kết quả sau sửa

Khi Windows để các mức scale như:

- `125%`
- `150%`
- `175%`
- hoặc mức khác

thì:

- ảnh preview
- vùng crop
- template lưu ra
- điểm match

sẽ bám theo `logical size` mà người dùng thực sự nhìn thấy trên màn hình.

Chỉ riêng bước click mới đổi ngược về `physical pixels` để Windows click đúng vị trí thật.

## Nguyên tắc rút ra

Nếu app có:

- chụp màn hình
- crop vùng bằng chuột
- xử lý ảnh
- rồi click lại lên desktop

thì nên chọn **một hệ tọa độ chuẩn** cho toàn bộ pipeline.

Ở case này, hệ đúng nhất là:

- `logical pixels` cho UI, crop, preview, template matching
- `physical pixels` chỉ dùng ở lớp cuối cùng khi click chuột thật

Đây là cách ổn định nhất để chạy đúng trên mọi mức scale của Windows.
