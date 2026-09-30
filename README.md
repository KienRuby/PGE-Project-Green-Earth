# PGE – Project Green Earth

PGE – Project Green Earth là dự án game hành động 2D trên Unity, thiết kế theo màn hình dọc. Người chơi chiến đấu với các đợt quái và boss, thu thập tài nguyên, nâng cấp nhân vật và mở khóa nội dung qua các chapter.

## Tính năng chính

- Nhân vật di chuyển, tự tìm mục tiêu và bắn; có hệ thống kỹ năng và lên cấp trong trận.
- Các đợt quái, boss, vật phẩm rơi và hiệu ứng chiến đấu.
- Hệ thống nâng cấp Lab, chipset, buddy drone, shop, nhiệm vụ và phần thưởng đăng nhập.
- Lưu tiến trình cục bộ; dự án cũng tích hợp Unity Authentication và Cloud Save cho chức năng tài khoản/đồng bộ.

## 1. Tải mã nguồn và chuẩn bị

Bạn cần máy tính cài **Unity Hub**, **Unity Editor 2022.3.62f2** và **Git**. Khi cài Editor bằng Unity Hub, chọn thêm **Android Build Support**, gồm **Android SDK & NDK Tools** và **OpenJDK** nếu muốn tạo bản Android. Lần mở đầu cần Internet để Unity tải các package; Git phải hoạt động trong dòng lệnh vì dự án có package lấy từ Git URL.

1. Vào [repository trên GitHub](https://github.com/KienRuby/PGE-Project-Green-Earth), chọn nhánh **main**.
2. Chọn một trong hai cách tải:
   - **Có Git:** mở terminal và chạy:

     ```bash
     git clone https://github.com/KienRuby/PGE-Project-Green-Earth.git
     ```

   - **Không dùng Git:** nhấn **Code → Download ZIP**, rồi giải nén ZIP vào một thư mục trên máy. Vẫn nên cài Git để Unity tải các package nói trên.
3. Xác nhận thư mục vừa tải có ba thư mục `Assets`, `Packages` và `ProjectSettings`. Đây là **thư mục gốc** cần chọn trong Unity Hub; đừng chọn riêng `Assets` hoặc một file `.sln`.

Repository chứa **mã nguồn Unity**, không phải file cài game. Muốn chơi trên điện thoại, hãy build APK theo mục 3.

## 2. Mở và chạy thử trong Unity

1. Trong **Unity Hub → Projects**, chọn **Add / Add project from disk** và trỏ tới thư mục gốc ở trên. Mở dự án bằng **2022.3.62f2**.
2. Chờ Unity import asset và tải package xong. Lần đầu có thể mất vài phút. Nếu Unity hỏi cài module Android còn thiếu, hãy thêm module cho đúng phiên bản Editor trong Unity Hub.
3. Mở **Window → General → Console**. Chỉ tiếp tục khi các lỗi biên dịch màu đỏ đã được xử lý; Unity không thể chạy hoặc build game khi script chưa biên dịch xong.
4. Trong cửa sổ **Project**, mở `Assets/Scenes/MainMenu.unity`, rồi nhấn **Play**. Bấm vào tab **Game** để xem game, dùng chuột để thao tác giao diện và **WASD** hoặc phím mũi tên để di chuyển.
5. Nhấn **Play** lần nữa để thoát chế độ thử trước khi build. Những thay đổi thực hiện trong Play Mode thường không được lưu vào scene.

## 3. Build và cài game

### Tạo APK Android để chơi thử

1. Trong Unity, mở **File → Build Settings**. Chọn **Android**, nhấn **Switch Platform** nếu nút này đang hiện. Chờ Unity chuyển nền tảng xong.
2. Kiểm tra **Scenes In Build**: `MainMenu` ở đầu danh sách và được đánh dấu; tiếp theo là `GamePlay`, `GenMine`, `TowerDef`, `Loading`. Đây là danh sách đã cấu hình trong dự án. Nếu thiếu scene, mở scene đó rồi bấm **Add Open Scenes** hoặc kéo từ cửa sổ Project vào danh sách.
3. Bỏ chọn **Export Project** và **Build App Bundle (Google Play)** để Unity xuất **APK** trực tiếp. Có thể bật **Development Build** nếu muốn xem log khi thử nghiệm.
4. Nhấn **Build**, chọn tên file như `Builds/Android/PGE-Green-Earth.apk` trong thư mục dự án, rồi chờ Unity báo build thành công. Thư mục `Builds/` được Git bỏ qua, nên APK không tự xuất hiện trên GitHub.
5. Chép APK sang điện thoại Android, mở file để cài và cho phép cài từ nguồn này nếu Android hỏi. Mở ứng dụng **PGE Green Earth** để chơi. Điện thoại cần Android API 24 trở lên theo cấu hình hiện tại của dự án.

Nếu muốn Unity tự cài sau khi build, bật **USB debugging** trên điện thoại, nối USB, chấp nhận yêu cầu cho phép gỡ lỗi trên điện thoại rồi chọn **Build And Run** trong Build Settings. Chọn đúng thiết bị ở **Run Device** nếu có nhiều thiết bị.

### Tạo AAB cho Google Play

Trong **Build Settings → Android**, bật **Build App Bundle (Google Play)** rồi nhấn **Build** để tạo file `.aab`. Trước khi phát hành, vào **Edit → Project Settings → Player → Android → Publishing Settings** để cấu hình **keystore/key alias** riêng; cấu hình ký phát hành hiện chưa được điền trong repository. Đồng thời kiểm tra **Package Name**, version/version code, cấu hình đăng nhập, Cloud Save và quảng cáo của môi trường phát hành. Build APK thử nghiệm ở trên không thay thế các bước này.

### Tạo bản Windows để thử trên PC (tùy chọn)

Trong **File → Build Settings**, chọn **PC, Mac & Linux Standalone**, đặt **Target Platform: Windows** và **Architecture: x86_64**, nhấn **Switch Platform**, rồi **Build** vào một thư mục riêng trong `Builds/`. Chạy file `.exe` Unity tạo ra. Dự án thiết kế cho màn hình dọc và cảm ứng; trên PC dùng chuột cùng **WASD** hoặc phím mũi tên. Một số chức năng dịch vụ di động có thể cần cấu hình hoặc thiết bị Android để hoạt động đầy đủ.

## 4. Cách chơi từ màn hình chính

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

## 5. Các chế độ chơi

| Chế độ | Cách vào và mục tiêu |
| --- | --- |
| **Chapter** | Chọn chapter rồi nhấn **Start**; tiêu hao Energy, vượt qua các wave và boss để mở tiến trình. |
| **Daily Gem Mine** | Khi nút Gem Mine được mở theo tiến trình chapter, chọn màn trong bảng Gem Mine rồi nhấn **Start**. Có 5 màn mở tuần tự; mặc định 5 lượt vào mỗi ngày, mỗi lần bắt đầu tốn 1 lượt. |
| **Tower Def** | Khi nút Tower Def được mở theo tiến trình chapter, chọn một màn đã mở. Đặt và nâng cấp công trình để bảo vệ cổng qua các wave; cổng bị phá thì thua. |

Trong **Tower Def**, chạm ô trống để mở bảng xây **turret** hoặc **generator**; chạm công trình đã đặt để nâng cấp. Chạm cổng để xem các lựa chọn nâng cấp/sửa chữa. Chế độ này dùng **Gold** riêng trong trận: turret khởi điểm tốn **50 Gold**, generator tốn **60 Gold**. Hoàn thành toàn bộ wave để thắng màn.

## 6. Nâng cấp và tài nguyên

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

## 7. Nếu không mở hoặc build được

| Hiện tượng | Kiểm tra |
| --- | --- |
| Unity không tìm thấy package lấy từ Git URL | Kiểm tra Git đã cài và có trong `PATH`, máy có Internet; mở lại dự án để Unity hoàn tất tải package. Xem `Packages/manifest.json` để biết nguồn package. |
| Console báo thiếu `GooglePlayGames` hoặc `Unity.Services` | Chờ Unity tải và import package xong; kiểm tra các package nhúng trong `Packages/` và mục **Window → Package Manager**. Xử lý lỗi đỏ trước khi nhấn Play/Build. |
| Console báo `.meta` không có GUID hợp lệ | Nhánh `main` hiện có bốn file `Assets/Resources/Prefabs/PlayerHitVFX_Skin1..4.prefab.meta` với GUID không hợp lệ; Unity sẽ bỏ qua prefab tương ứng. Đây là lỗi asset cần sửa trong dự án trước khi dùng các prefab đó. |
| Build Settings không có Android hoặc báo thiếu SDK/NDK/JDK | Trong Unity Hub, thêm **Android Build Support**, **Android SDK & NDK Tools** và **OpenJDK** cho Editor 2022.3.62f2. |
| **Build And Run** không thấy điện thoại | Bật USB debugging, xác nhận quyền gỡ lỗi trên điện thoại, kiểm tra cáp USB và mục **Run Device**. Có thể chọn **Build** rồi chép APK thủ công. |
| Không cài được APK | Xem thông báo trên điện thoại, cho phép cài từ nguồn của ứng dụng đang mở APK; nếu đã cài bản ký bằng khóa khác, cần gỡ bản cũ hoặc cài bản được ký cùng khóa. |

Các tính năng đăng nhập, lưu cloud và quảng cáo có thể cần cấu hình dịch vụ riêng để hoạt động trên bản build của bạn. Nếu Console còn lỗi, mở dòng lỗi đầu tiên và xử lý nguyên nhân trước; các lỗi sau thường là hệ quả.

## Cấu trúc thư mục

| Đường dẫn | Nội dung |
| --- | --- |
| `Assets/Scenes/` | Các scene của game |
| `Assets/Scripts/` | Mã gameplay, UI, lưu dữ liệu và các hệ thống khác |
| `Assets/Resources/`, `Assets/Prefabs/` | Asset và prefab được game sử dụng |
| `Assets/Editor/` | Công cụ Editor và kiểm thử |
| `Packages/` | Cấu hình package và các package nhúng |
| `ProjectSettings/` | Cấu hình dự án Unity |

## Tài liệu tham khảo

- [Unity 2022.3: Build ứng dụng Android](https://docs.unity3d.com/2022.3/Documentation/Manual/android-BuildProcess.html)
- [Unity 2022.3: Package lấy từ Git URL](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-ui-giturl.html)
- [Unity 2022.3: Keystore Android](https://docs.unity3d.com/2022.3/Documentation/Manual/android-keystore-load.html)
