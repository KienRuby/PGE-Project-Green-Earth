# PGE – Project Green Earth

PGE – Project Green Earth là dự án game hành động 2D trên Unity, thiết kế theo màn hình dọc. Người chơi chiến đấu với các đợt quái và boss, thu thập tài nguyên, nâng cấp nhân vật và mở khóa nội dung qua các chapter.

## Tính năng chính

- Nhân vật di chuyển, tự tìm mục tiêu và bắn; có hệ thống kỹ năng và lên cấp trong trận.
- Các đợt quái, boss, vật phẩm rơi và hiệu ứng chiến đấu.
- Hệ thống nâng cấp Lab, chipset, buddy drone, shop, nhiệm vụ và phần thưởng đăng nhập.
- Lưu tiến trình cục bộ; dự án cũng tích hợp Unity Authentication và Cloud Save cho chức năng tài khoản/đồng bộ.

## Mở và chạy dự án

1. Cài **Unity Editor 2022.3.62f2** qua Unity Hub.
2. Mở thư mục gốc của repository bằng Unity Hub và đợi Unity import asset cùng các package trong `Packages/manifest.json`.
3. Mở scene `Assets/Scenes/MainMenu.unity` và nhấn **Play**.

Các scene có trong Build Settings: `MainMenu`, `GamePlay`, `GenMine`, `TowerDef` và `Loading`.

## Cấu trúc thư mục

| Đường dẫn | Nội dung |
| --- | --- |
| `Assets/Scenes/` | Các scene của game |
| `Assets/Scripts/` | Mã gameplay, UI, lưu dữ liệu và các hệ thống khác |
| `Assets/Resources/`, `Assets/Prefabs/` | Asset và prefab được game sử dụng |
| `Assets/Editor/` | Công cụ Editor và kiểm thử |
| `Packages/` | Cấu hình package và các package nhúng |
| `ProjectSettings/` | Cấu hình dự án Unity |

Để build trên Android hoặc iOS, hãy cài module nền tảng tương ứng trong Unity Hub và cấu hình các dịch vụ ngoài (như đăng nhập, Cloud Save hoặc quảng cáo) cho môi trường phát hành của bạn.
