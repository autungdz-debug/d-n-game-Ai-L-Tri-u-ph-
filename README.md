# Game Ai Là Triệu Phú (Who Wants to Be a Millionaire) - WPF & C# (.NET 9 / Visual Studio 2022)

Dự án game **"Ai Là Triệu Phú"** hoàn chỉnh được xây dựng bằng **WPF (Frontend)** và **C# (.NET 9 - Backend)**, tương thích hoàn toàn với **Visual Studio 2022**.

---

## 🌟 Tính Năng Nổi Bật

1. **Giao diện chuẩn Studio truyền hình**:
   - Thiết kế phong cách trường quay VTV3 với tông màu xanh bóng đêm huyền bí (Midnight Navy), hiệu ứng ánh đèn spotlight và viền neon vàng kim (Gold Glow).
   - Khung câu hỏi lục giác vát cạnh hiện đại.
   - 4 ô phương án A, B, C, D với hiệu ứng tương tác trực quan:
     - **Hover**: Nổi bật đường viền.
     - **Chọn đáp án**: Chuyển sang màu vàng cam kịch tính và giữ nhịp hồi hộp trong 2 giây y như trên truyền hình.
     - **Chính xác**: Nhấp nháy xanh lục bảo rực rỡ.
     - **Sai**: Chuyển màu đỏ và đồng thời tự động phát sáng đáp án đúng màu xanh.

2. **Thang tiền thưởng 15 câu hỏi chính thức**:
   - Thang bậc từ Câu 1 (200.000 đ) đến Câu 15 (150.000.000 đ).
   - Đánh dấu 3 mốc quan trọng:
     - **Mốc 5**: 2.000.000 đ (Bảo toàn giải thưởng mốc 1).
     - **Mốc 10**: 22.000.000 đ (Bảo toàn giải thưởng mốc 2).
     - **Mốc 15**: 150.000.000 đ (Đăng quang Triệu phú).
   - Cột thang tiền thưởng bên phải tự động phát sáng câu hiện tại và đánh dấu các câu đã vượt qua.

3. **5 Quyền trợ giúp đặc biệt (Lifelines)**:
   - **50:50**: Máy tính loại bỏ ngẫu nhiên 2 phương án sai.
   - **📞 Gọi điện thoại cho người thân**: Cho phép chọn kết nối với 1 trong 4 nhân vật (Bác sĩ, Giáo sư, Kỹ sư IT, Bạn thân) với hội thoại tư vấn và chỉ số tin cậy.
   - **👥 Hỏi ý kiến khán giả trong trường quay**: Mô phỏng biểu đồ cột phần trăm bình chọn trực quan từ 100 khán giả trường quay.
   - **👨‍🏫 Hỏi ý kiến tổ tư vấn tại chỗ**: Nhận phân tích, lập luận chi tiết từ 3 chuyên gia.
   - **🔄 Đổi câu hỏi**: Thay thế câu hỏi hiện tại bằng một câu hỏi mới cùng cấp độ.

4. **Đồng hồ đếm ngược áp lực thời gian**:
   - Đếm ngược 30 giây (có thể tắt/bật tùy chọn trước khi vào trận).
   - Tự động chuyển màu đỏ cảnh báo và phát âm thanh tích tắc khi còn dưới 5 giây.

5. **Dừng cuộc chơi (Walk Away)**:
   - Người chơi có thể bảo toàn trọn vẹn số tiền thưởng đã đạt được ở câu hỏi trước đó.

6. **Hệ thống âm thanh kịch tính tự động (SoundGenerator)**:
   - Tự động sinh và lưu trữ các hiệu ứng âm thanh chuẩn WAV (tiếng đếm ngược, tiếng chốt phương án, âm thanh đúng, âm thanh sai, nhạc chúc mừng mốc an toàn và chiến thắng).
   - Tích hợp nút bật/tắt âm thanh (Mute) ngay trên thanh tiêu đề.
   - Chọn âm thanh đọc bởi MC

7. **Bảng vàng kỷ lục gia (Hall of Fame / Leaderboard)**:
   - Lưu trữ và tự động xếp hạng Top 10 người chơi có tiền thưởng cao nhất vào tệp `highscores.json`.

8. **Trình quản lý & mở rộng ngân hàng câu hỏi (Question Bank Manager)**:
   - Ngân hàng tích hợp sẵn **60+ câu hỏi Tiếng Việt** đa dạng lĩnh vực (Lịch sử, Địa lý, Văn học, Khoa học, Âm nhạc, Đời sống).
   - Giao diện tra cứu, lọc theo cấp độ từ 1 đến 15, tìm kiếm từ khóa.
   - Cho phép thêm mới hoặc xóa câu hỏi trực tiếp trên giao diện, tự động đồng bộ vào tệp `Resources/questions.json`.

---

## 📁 Cấu Trúc Dự Án

```text
AiLaTrieuPhu/
├── AiLaTrieuPhu.sln                <- Tệp Solution mở trực tiếp bằng Visual Studio 2022
└── AiLaTrieuPhu/
    ├── AiLaTrieuPhu.csproj         <- Cấu hình .NET 9 WPF
    ├── App.xaml / App.xaml.cs      <- Khởi tạo ứng dụng và nạp tài nguyên
    ├── MainWindow.xaml / .cs       <- Màn hình chính, sân khấu, đồng hồ và thang giải thưởng
    ├── Models/
    │   ├── Question.cs             <- Cấu trúc dữ liệu câu hỏi, phương án, đáp án đúng
    │   ├── PrizeLevel.cs           <- Cấu trúc 15 bậc tiền thưởng và mốc quan trọng
    │   ├── HighScore.cs            <- Cấu trúc thông tin điểm kỷ lục người chơi
    │   └── LifelineType.cs         <- Enum các quyền trợ giúp
    ├── Services/
    │   ├── GameEngine.cs           <- Máy trạng thái quản lý toàn bộ logic trò chơi
    │   ├── QuestionService.cs      <- Đọc/ghi, lọc và cấp phát câu hỏi ngẫu nhiên
    │   ├── SoundService.cs         <- Quản lý phát âm thanh hiệu ứng
    │   ├── SoundGenerator.cs       <- Bộ sinh sóng âm thanh WAV tự động
    │   └── HighScoreService.cs     <- Quản lý lưu trữ bảng xếp hạng
    ├── Views/
    │   ├── AudienceDialog.xaml     <- Cửa sổ biểu đồ khán giả trường quay
    │   ├── PhoneDialog.xaml        <- Cửa sổ gọi điện thoại cho người thân
    │   ├── ExpertsDialog.xaml      <- Cửa sổ hỏi ý kiến tổ tư vấn
    │   ├── HighScoreWindow.xaml    <- Cửa sổ hiển thị bảng vàng kỷ lục
    │   ├── HowToPlayDialog.xaml    <- Cửa sổ hiển thị hướng dẫn luật chơi
    │   └── QuestionManagerWindow.xaml <- Cửa sổ quản lý ngân hàng câu hỏi
    └── Resources/
        ├── Styles.xaml             <- Màu sắc, brush gradient, style nút bấm studio
        └── questions.json          <- Ngân hàng dữ liệu câu hỏi chuẩn UTF-8
```

---

## 🚀 Hướng Dẫn Mở & Chạy Dự Án Trên Visual Studio 2022

1. **Mở dự án**:
   - Khởi động **Visual Studio 2022**.
   - Chọn **Open a project or solution**.
   - Trỏ tới đường dẫn:
     `"D:\BÁO CÁO_ GAME_AI_LÀ_TRIỆU_PHÚ\AiLaTrieuPhu"`
   - Hoặc nhấp đúp trực tiếp vào tệp `AiLaTrieuPhu.sln`.

2. **Chạy trò chơi**:
   - Nhấn phím **F5** (Debug) hoặc **Ctrl + F5** (Run without debugging).
   - Visual Studio sẽ tự động biên dịch và mở màn hình trò chơi.

3. **Chạy nhanh từ dòng lệnh (nếu không mở Visual Studio)**:
   ```bash
   dotnet run --project D:\BÁO CÁO_ GAME_AI_LÀ_TRIỆU_PHÚ\AiLaTrieuPhu\AiLaTrieuPhu.csproj
   ```
