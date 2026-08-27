# Báo cáo trạng thái Lab và tiền tệ khi build APK

Ngày kiểm tra: 2026-08-28

## Kết luận

Với một bản cài sạch (ứng dụng chưa có `PlayerPrefs`), cấu hình hiện tại cho kết quả:

| Dữ liệu | Giá trị ban đầu |
|---|---:|
| Chỉ số Lab bị khóa | 16/16 |
| Chỉ số Lab đã mở | 0/16 |
| Level lưu cho mỗi chỉ số Lab | 0 (trạng thái khóa) |
| Data Chips | 1.000 |
| Red Gems | 1.000 |
| Energy | 100/100 |
| Advance Stones | 0 |

Các chỉ số Lab **không bắt đầu ở level 1**. `startingLevel = 1` chỉ có hiệu lực khi `startsUnlocked` được bật; trong scene `MainMenu`, cả 16 mục đều có `startsUnlocked = false`, nên giá trị mặc định thực tế là level 0/khóa.

## Nguyên nhân dễ gây hiểu nhầm khi thử APK

Android giữ lại `PlayerPrefs` khi cài APK mới đè lên ứng dụng có cùng package ID `com.pge.greenearth`. Vì vậy:

- Cài đè/cập nhật APK: giữ level Lab và tiền tệ của lần chơi trước.
- Gỡ ứng dụng hoặc Clear Storage rồi cài lại: dùng các giá trị fresh-install trong bảng trên.
- Chạy trong Unity Editor: `ChipManager` tự bật Test Mode và có thể hiển thị số thử nghiệm rất lớn.
- Build APK: nhánh mã ngoài `UNITY_EDITOR` cưỡng chế tắt Test Mode và chế độ vô hạn.

Không thêm `PlayerPrefs.DeleteAll()` lúc khởi động vì thao tác đó sẽ xóa tiến trình thật của người chơi sau mỗi lần cập nhật hoặc mở game.

## Kiểm tra đã thực hiện

1. Kiểm tra `PlayerDataService`: fallback Lab là level 0; Data Chips 1.000; Red Gems 1.000; Energy 100; Advance Stones 0.
2. Kiểm tra `LabUpgradeController`: item chỉ nhận level khởi đầu từ 1 khi `startsUnlocked = true`; nếu không thì mặc định level 0.
3. Kiểm tra scene `Assets/Scenes/MainMenu.unity`: có 16 item Lab và toàn bộ `startsUnlocked = false`.
4. Chạy phép kiểm tra fresh-install tĩnh trên scene và mã nguồn: 16 khóa, 0 mở, các số dư đúng như bảng.
5. Thêm regression test `ApkFreshInstall_AllLabItemsStartLocked_AndReleaseBalancesAreCorrect` để kiểm tra đồng thời scene, toàn bộ 16 item và bốn loại tài nguyên. Test có sao lưu/phục hồi `PlayerPrefs`, không phá dữ liệu Editor.
6. Unity đã biên dịch lại `Assembly-CSharp-Editor` thành công (`Tundra build success`), không có lỗi C# mới. Test Runner chưa được chạy trong giao diện vì thao tác Computer Use đã được người dùng dừng bằng phím Escape.

## Cách xác minh trên thiết bị Android

1. Gỡ bản game cũ hoặc vào App info > Storage > Clear storage.
2. Cài APK mới.
3. Mở Lab: cả 16 chỉ số phải hiện khóa, chưa có mục nào ở level 1.
4. Kiểm tra số dư: 1.000 Data Chips, 1.000 Red Gems, 100 Energy và 0 Advance Stones.
5. Sau đó cài một APK mới đè lên bản này để xác nhận tiến trình vẫn được giữ nguyên.

## Thay đổi phòng ngừa

Đã bổ sung kiểm thử hồi quy tại `Assets/Editor/PGEGameLogicTests.cs`. Không thay đổi logic runtime vì logic fresh-install hiện tại đã đúng; thay đổi runtime để cưỡng chế reset sẽ gây mất save người chơi.
