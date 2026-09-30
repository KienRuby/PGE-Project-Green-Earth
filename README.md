# PGE – Project Green Earth

PGE – Project Green Earth là dự án game hành động 2D trên Unity, thiết kế theo màn hình dọc. Người chơi chiến đấu với các đợt quái và boss, thu thập tài nguyên, nâng cấp nhân vật và mở khóa nội dung qua các chapter.

## Tính năng chính

- Nhân vật di chuyển, tự tìm mục tiêu và bắn; có hệ thống kỹ năng và lên cấp trong trận.
- Các đợt quái, boss, vật phẩm rơi và hiệu ứng chiến đấu.
- Hệ thống nâng cấp Lab, chipset, buddy drone, shop, nhiệm vụ và phần thưởng đăng nhập.
- Lưu tiến trình cục bộ; dự án cũng tích hợp Unity Authentication và Cloud Save cho chức năng tài khoản/đồng bộ.

## Hướng dẫn chơi nhanh

1. Từ màn hình chính, mở mục **Chiến đấu / Chapter** ở thanh điều hướng dưới cùng.
2. Dùng nút mũi tên để chọn chapter. Chapter bị khóa chưa thể vào; nút **Start** hiển thị lượng **Energy** cần dùng. Nhấn **Start** khi đủ Energy để bắt đầu trận.
3. Di chuyển để né quái và tiếp cận vật phẩm rơi. Nhân vật tự tìm mục tiêu và bắn, nên bạn không cần giữ nút bắn.
4. Nhặt EXP để lên cấp. Khi bảng chọn chipset xuất hiện, chạm một thẻ để nhận hoặc nâng cấp kỹ năng cho trận hiện tại.
5. Sống sót qua các wave và đánh bại boss để hoàn thành chapter, nhận thưởng và mở tiến trình tiếp theo.

### Điều khiển

| Nền tảng | Thao tác |
| --- | --- |
| Điện thoại / màn hình cảm ứng | Chạm và kéo trong vùng điều khiển để di chuyển bằng joystick ảo; thả tay để dừng. |
| PC trong Unity Editor | Dùng **WASD** hoặc **phím mũi tên** để di chuyển; dùng chuột để bấm các nút và thẻ trên giao diện. |

Vũ khí tự ngắm và tự bắn khi có mục tiêu phù hợp. Trong **Settings**, bạn có thể đổi joystick động/cố định, bật tắt hình joystick, âm nhạc, hiệu ứng âm thanh, số sát thương và rung màn hình.

### Trong trận đấu

- Thanh máu cho biết khả năng sống sót; thanh EXP đầy sẽ mở bảng chọn chipset. Trận tạm dừng trong lúc bạn chọn thẻ.
- Nút **Draw again / Quay lại** đổi bộ thẻ gợi ý. Mỗi lần tốn **20 Red Gems**, tối đa **2 lần cho mỗi cấp** theo cấu hình hiện tại.
- Nút **Pause** mở các mục **Stats**, **Chipset**, **Artifact**; chọn **Resume** để tiếp tục hoặc **Home** để về màn chính sau khi xác nhận.
- Nếu nhân vật gục, màn hồi sinh cho phép hồi sinh **một lần trong trận** bằng **200 Red Gems** hoặc quảng cáo thưởng khi dịch vụ quảng cáo khả dụng. Bạn cũng có thể bỏ qua để xem kết quả và nhận phần thưởng của lượt chơi.

## Các chế độ chơi

| Chế độ | Cách vào và mục tiêu |
| --- | --- |
| **Chapter** | Chọn chapter rồi nhấn **Start**; tiêu hao Energy, vượt qua các wave và boss để mở tiến trình. |
| **Daily Gem Mine** | Khi nút Gem Mine được mở theo tiến trình chapter, chọn màn trong bảng Gem Mine rồi nhấn **Start**. Có 5 màn mở tuần tự; mặc định 5 lượt vào mỗi ngày, mỗi lần bắt đầu tốn 1 lượt. |
| **Tower Def** | Khi nút Tower Def được mở theo tiến trình chapter, chọn một màn đã mở. Đặt và nâng cấp công trình để bảo vệ cổng qua các wave; cổng bị phá thì thua. |

Trong **Tower Def**, chạm ô trống để mở bảng xây **turret** hoặc **generator**; chạm công trình đã đặt để nâng cấp. Chạm cổng để xem các lựa chọn nâng cấp/sửa chữa. Chế độ này dùng **Gold** riêng trong trận: turret khởi điểm tốn **50 Gold**, generator tốn **60 Gold**. Hoàn thành toàn bộ wave để thắng màn.

## Nâng cấp và tài nguyên

Thanh điều hướng màn chính có năm mục:

| Mục | Dùng để làm gì |
| --- | --- |
| **Shop / Cửa hàng** | Xem hộp và các gói tài nguyên. |
| **Lab / Phòng thí nghiệm** | Chọn ô chỉ số và dùng Data Chips để nâng cấp sức mạnh lâu dài. |
| **Chapter / Chiến đấu** | Chọn chapter và vào các chế độ chơi đã mở. |
| **Chipset** | Xem bộ chipset, trang bị, tăng cấp và nâng bậc khi đủ điều kiện/tài nguyên. |
| **Buddy / Đồng đội** | Xem drone, trang bị đội hình và nâng cấp buddy. |

| Tài nguyên | Công dụng chính |
| --- | --- |
| **Energy** | Dùng để bắt đầu chapter; hồi dần theo thời gian. |
| **Data Chips** | Dùng cho nâng cấp Lab và chipset. |
| **Red Gems** | Dùng để đổi lựa chọn chipset khi lên cấp và hồi sinh trong trận. |
| **Advance Stones** | Dùng khi nâng chipset lên bậc cao theo yêu cầu của từng chipset. |
| **Chipset Boxes / Drone Boxes** | Hộp vật phẩm trong hệ thống Shop và bộ sưu tập tương ứng. |

**Gợi ý:** Trước khi vào chapter khó, hãy kiểm tra Lab, chipset và buddy đã trang bị. Trong trận, ưu tiên né đòn, nhặt EXP và chọn kỹ năng phù hợp với bộ trang bị hiện có. Ở Tower Def, cân đối Gold giữa xây thêm công trình và bảo vệ cổng.

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
