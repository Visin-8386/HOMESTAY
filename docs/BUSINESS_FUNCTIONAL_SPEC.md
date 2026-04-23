# BUSINESS FUNCTIONAL SPECIFICATION

Document ID: BFS-HOMESTAY-001  
Version: 1.0  
Date: 2026-04-23  
Status: Approved for Development Baseline

---

## 1. Purpose

Tai lieu nay mo ta day du nghiep vu chuc nang cho nen tang Homestay gom 3 khoi:

- Flutter Mobile App (end-user/host/admin mobile)
- Nhom1 API (.NET 8)
- WebHS (Web MVC + admin operations)

Muc tieu:

- Dong bo giua Business, Dev, QA, va Product Owner
- Lam baseline cho estimate, implementation, testing, UAT
- Giam ambiguity trong yeu cau truoc khi release

---

## 2. Business Goals

1. So hoa quy trinh dat phong homestay end-to-end.
2. Tang toc do booking confirmation va giam thao tac thu cong.
3. Tang doanh thu host qua quan ly ton kho phong, gia linh hoat, khuyen mai.
4. Tang muc do tin cay he thong qua thanh toan, thong bao, va audit.
5. Nang cao trai nghiem khach hang bang AI support va realtime interaction.

---

## 3. Scope

### 3.1 In Scope

- User lifecycle (register/login/profile/security)
- Role & permission (User, Host, Admin)
- Homestay catalog management
- Search/filter/sort/map discovery
- Booking lifecycle
- Payment/deposit/callback
- Review/rating moderation
- Promotion/coupon
- Notification center
- Messaging + conversation
- Audio/video call signaling
- AI chat assistant
- Admin operation and reports

### 3.2 Out of Scope (Current Phase)

- Dynamic pricing by ML model
- Multi-currency settlement engine
- Auto-refund with external dispute workflow
- Full ERP integration

---

## 4. Stakeholders

- Product Owner: define priorities and business policy
- Business Analyst: formalize requirements and acceptance
- Engineering Team: implement and maintain system
- QA Team: verify function, regression, and UAT criteria
- Operations/Admin Team: monitor, moderate, and support
- Host: publish and operate listings
- User/Guest: search, book, pay, review

---

## 5. Role Matrix

| Capability | Guest (unauth) | User | Host | Admin |
|---|---:|---:|---:|---:|
| Browse homestay | Y | Y | Y | Y |
| Booking creation | N | Y | Y (for own trip) | Y |
| Manage own listings | N | N | Y | Y |
| Manage users | N | N | N | Y |
| Manage promotions | N | N | Limited | Y |
| Moderate reviews | N | N | N | Y |
| View platform reports | N | N | Limited | Y |

Legend: Y = allowed, N = not allowed.

---

## 6. Functional Modules

## M01. Identity & Access Management

### F-Auth-01 Register Account

- Objective: Tao tai khoan moi cho User/Host.
- Actors: Guest.
- Preconditions: Email/phone chua ton tai.
- Main Flow:
  1. Guest nhap thong tin dang ky.
  2. System validate format, uniqueness.
  3. System tao account in Pending/Active state theo policy.
  4. System gui OTP/email verification neu bat buoc.
- Alternative Flow:
  - AF1: Email trung -> tra loi loi business `EMAIL_ALREADY_EXISTS`.
  - AF2: OTP sai/het han -> yeu cau gui lai OTP.
- Business Rules:
  - BR-A01: Password min length >= 6 (co the thay doi theo env policy).
  - BR-A02: Email phai unique toan he thong.
  - BR-A03: Login only when account active.
- Acceptance Criteria:
  - AC1: Register thanh cong khi du lieu hop le.
  - AC2: Khong tao duplicate account.
  - AC3: OTP flow hoat dong dung expiry.

### F-Auth-02 Login + JWT

- Objective: Xac thuc va cap token.
- Actors: User/Host/Admin.
- Main Flow:
  1. User submit credential.
  2. System verify account + password + status.
  3. System issue access token + refresh token.
- Business Rules:
  - BR-A04: Token co expiry theo config.
  - BR-A05: Refresh token phai rotate sau moi lan refresh.
- Acceptance Criteria:
  - AC1: Login dung tra ve token + user profile summary.
  - AC2: Login sai tra message an toan, khong lo thong tin nhay cam.

### F-Auth-03 Role-based Authorization

- Objective: Bao dam moi API/screen dung quyen.
- Actors: System.
- Business Rules:
  - BR-A06: API admin chi role Admin duoc truy cap.
  - BR-A07: Host chi thao tac tren listing cua chinh minh.
- Acceptance Criteria:
  - AC1: Unauthorized tra 401/403 dung ngu canh.

---

## M02. User Profile & Security

### F-User-01 View/Update Profile

- Actors: User/Host/Admin.
- Scope: avatar, full name, phone, address, preferences.
- Rules:
  - BR-U01: User chi cap nhat profile cua minh.
  - BR-U02: Email update co verification lai neu policy bat.
- Acceptance:
  - AC1: Cap nhat thanh cong va phan hoi du lieu moi nhat.

### F-User-02 Change Password & Biometric

- Rules:
  - BR-U03: Can current password de doi password.
  - BR-U04: Biometric chi la factor tren device, khong thay the server auth.

---

## M03. Homestay Listing Management

### F-Home-01 Create Listing

- Actors: Host/Admin.
- Data Required:
  - title, description, address, geo location
  - base price, max guests, amenities
  - images/videos
- Main Flow:
  1. Host tao listing.
  2. System validate mandatory fields.
  3. System luu listing va set status (Draft/Pending/Published) theo policy.
- Rules:
  - BR-H01: Listing phai co >= 1 image hop le.
  - BR-H02: Gia phong > 0.
- Acceptance:
  - AC1: Listing moi xuat hien tren host dashboard sau khi save.

### F-Home-02 Update/Delete Listing

- Rules:
  - BR-H03: Khong cho xoa listing neu co booking active (tu policy).
  - BR-H04: Update listing ghi nhan audit trail.

### F-Home-03 Availability Calendar

- Objective: Quan ly ngay mo/dong va ton kho.
- Rules:
  - BR-H05: Date range cannot overlap with closed dates.
  - BR-H06: Booking chi tao duoc khi inventory con.

---

## M04. Search, Discovery, and Map

### F-Search-01 Search Homestay

- Filters:
  - location, date range, guests, price range, amenities, rating
- Rules:
  - BR-S01: Ket qua tra ve da loai bo listing inactive.
  - BR-S02: Paging default va max page size theo API policy.
- Acceptance:
  - AC1: Search phan hoi trong SLA da dinh.

### F-Search-02 Sort & Recommendation

- Sort options: price asc/desc, rating desc, newest.
- Recommendation: theo xu huong, diem danh gia, booking frequency.

---

## M05. Booking Lifecycle

### F-Book-01 Create Booking

- Actors: User.
- Preconditions:
  - User da login.
  - Listing available trong date range.
- Main Flow:
  1. User chon check-in/check-out/guests.
  2. System kiem tra availability.
  3. System tinh tong tien (gia + phi - khuyen mai).
  4. System tao booking status = PendingPayment/PendingConfirmation.
- Rules:
  - BR-B01: checkOut > checkIn.
  - BR-B02: guests <= maxGuests cua listing.
  - BR-B03: Khong cho double booking cung room/date.
- Acceptance:
  - AC1: Booking tao thanh cong va co ma booking unique.

### F-Book-02 Booking State Machine

- States de xuat:
  - Draft
  - PendingPayment
  - PendingConfirmation
  - Confirmed
  - InStay
  - Completed
  - CancelledByUser
  - CancelledByHost
  - Expired
- Rules:
  - BR-B04: Chi transition hop le theo state machine.
  - BR-B05: Moi transition ghi log audit + timestamp.

### F-Book-03 Cancel Booking

- Rules:
  - BR-B06: Ap dung cancellation policy theo moc thoi gian.
  - BR-B07: Neu da thanh toan, xu ly refund theo payment policy.

---

## M06. Payment & Settlement

### F-Pay-01 Initiate Payment

- Methods: PayPal (sandbox/prod), options mo rong.
- Rules:
  - BR-P01: So tien thanh toan phai trung voi booking snapshot.
  - BR-P02: Moi payment transaction co idempotency key.

### F-Pay-02 Payment Callback/Webhook

- Main Flow:
  1. Gateway gui callback.
  2. System verify signature + status.
  3. System update payment + booking state atomically.
- Rules:
  - BR-P03: Callback duplicate khong duoc tao side effect lan 2.
  - BR-P04: Khi payment fail -> booking rollback theo policy.

### F-Pay-03 Refund

- Rules:
  - BR-P05: Refund full/partial theo cancellation policy.
  - BR-P06: Refund phai co reason code.

---

## M07. Review & Rating

### F-Review-01 Submit Review

- Preconditions: User da hoan thanh stay.
- Rules:
  - BR-R01: Chi user da booking completed moi duoc review.
  - BR-R02: 1 booking toi da 1 review (hoac update policy).
- Acceptance:
  - AC1: Rating trung binh listing duoc cap nhat sau review hop le.

### F-Review-02 Moderation

- Actors: Admin.
- Rules:
  - BR-R03: Admin co the an/hien review vi pham.

---

## M08. Promotion & Pricing

### F-Promo-01 Coupon Validation

- Rules:
  - BR-PR01: Coupon hop le theo date, quota, min spend, scope.
  - BR-PR02: Coupon khong stack neu policy khong cho phep.

### F-Promo-02 Promotion Management

- Actors: Admin/Host (theo quyen).
- Acceptance:
  - AC1: Promotion active anh huong dung checkout price.

---

## M09. Notification Center

### F-Noti-01 Event Notification

- Events:
  - booking created/confirmed/cancelled
  - payment success/fail
  - promo campaign
  - message incoming
- Channels: in-app, email (theo config).
- Rules:
  - BR-N01: Notification payload can truy vet theo entityId.

### F-Noti-02 Read/Unread

- Acceptance:
  - AC1: User mark read 1 item hoac all items.
  - AC2: Unread counter cap nhat real-time/near real-time.

---

## M10. Messaging & Conversation

### F-Chat-01 1-1 Conversation

- Actors: User, Host.
- Rules:
  - BR-C01: User chi chat voi host lien quan listing/booking.
  - BR-C02: Message phai luu lich su va co timestamp.

### F-Chat-02 Conversation Management

- Features: list, search, pin, unread count.

---

## M11. Audio/Video Call (Realtime)

### F-Call-01 Incoming/Outgoing Call

- Actors: User, Host.
- Dependencies: SignalR hub + WebRTC ICE config.
- Rules:
  - BR-CL01: Chi user authenticated moi join user group.
  - BR-CL02: Offer/Answer/ICE phai gan voi callId hop le.
- Acceptance:
  - AC1: Cuoc goi co the tao, chap nhan, tu choi, ket thuc dung trang thai.

---

## M12. AI Assistant

### F-AI-01 AI Chat Support

- Use cases:
  - huong dan tim phong
  - giai dap chinh sach dat/cancel
  - goi y diem den
- Rules:
  - BR-AI01: Khong de lo thong tin nhay cam trong prompt/log.
  - BR-AI02: Neu AI fail, he thong tra fallback message than thien.

---

## M13. Admin Operations

### F-Admin-01 User Governance

- Features: lock/unlock, role assignment, profile moderation.
- Rules:
  - BR-AD01: Role assignment phai co audit log.

### F-Admin-02 Listing Governance

- Features: approve/reject listing, content moderation.

### F-Admin-03 Booking Oversight

- Features: view all bookings, force status correction (limited).
- Rules:
  - BR-AD02: Force change can permission va ly do bat buoc.

### F-Admin-04 Reports & Dashboard

- Metrics:
  - GMV, booking volume, cancel rate, occupancy, top hosts
- Acceptance:
  - AC1: Dashboard load du lieu theo date range.

---

## 7. Cross-Module Business Rules

- BR-X01: Tat ca actions nhay cam phai co audit trail (who/when/what).
- BR-X02: Tat ca API mutating phai idempotent hoac co duplicate protection.
- BR-X03: Timezone mac dinh ap dung thong nhat toan he thong.
- BR-X04: Soft delete uu tien hon hard delete voi entity nghiep vu.
- BR-X05: Error response phai co errorCode on dinh de QA automate.

---

## 8. Non-Functional Requirements

### 8.1 Security

- JWT signed key managed by secret vault/env.
- TLS required for production traffic.
- Input validation + output encoding + anti-injection.
- Secrets khong duoc hard-code.

### 8.2 Performance

- P95 read API <= 800ms (khong tinh external gateway).
- P95 booking create <= 1200ms.
- Search API support pagination and index strategy.

### 8.3 Reliability

- Graceful fallback khi Redis unavailable.
- Retry policy cho external integrations.
- Structured logging + traceId correlation.

### 8.4 Observability

- Application logs, audit logs, error logs tach biet.
- Booking/payment lifecycle co event logs day du.

---

## 9. API Contract Expectations

Moi endpoint quan trong phai co:

- Request schema
- Response schema
- Validation errors
- Business error codes
- Authorization requirement
- Sample payload

Tai lieu OpenAPI/Swagger la nguon su that cho interface-level validation.

---

## 10. Data Dictionary (Core Entities)

- User: id, email, role, status, createdAt, lastLogin
- Homestay: id, hostId, title, price, address, status
- Booking: id, userId, homestayId, checkIn, checkOut, guests, status, totalAmount
- Payment: id, bookingId, amount, method, transactionRef, status
- Review: id, bookingId, userId, homestayId, rating, comment, status
- Promotion: id, code, type, value, startAt, endAt, quota, status
- Notification: id, userId, type, payload, isRead, createdAt
- Conversation/Message: id, participantIds, content, createdAt, readState

---

## 11. UAT Scenarios (Minimum)

1. Register -> Login -> Search -> Booking -> Payment -> Review success.
2. Invalid coupon khong lam thay doi tong tien.
3. Double booking prevention cho cung date range.
4. Payment callback duplicate khong doi state lan 2.
5. Host khong the sua listing cua host khac.
6. Admin co the moderate review vi pham.
7. SignalR call incoming/accept/reject/end dung state.
8. Redis down nhung auth + booking van hoat dong o muc chap nhan duoc.

---

## 12. Release Readiness Checklist

- [ ] Tat ca AC trong module chinh da pass
- [ ] Regression suite pass
- [ ] Security scan pass (secret, dependency, basic SAST)
- [ ] Monitoring dashboard va alert rule da cau hinh
- [ ] Backup/rollback plan da xac nhan
- [ ] Business sign-off tu Product Owner

---

## 13. Open Points

1. Chot cancellation policy chi tiet theo moc gio.
2. Chot logic chia se quyen khuyen mai giua Host va Admin.
3. Chot SLA cho AI response va fallback strategy.
4. Chot co che escalation khi payment callback timeout.

---

## 14. Traceability Mapping (Sample)

| Requirement ID | Module | API/Screen Area | Test Case Group |
|---|---|---|---|
| F-Auth-02 | IAM | AuthController/LoginScreen | TC-AUTH |
| F-Book-01 | Booking | BookingsController/BookingScreen | TC-BOOK |
| F-Pay-02 | Payment | PaymentController/PaymentService | TC-PAY |
| F-Call-01 | Realtime | CallHub/CallScreen | TC-CALL |
| F-Admin-01 | Admin | AdminController/AdminUsersScreen | TC-ADMIN |

---

## 15. Sign-off

- Product Owner: Pending
- Tech Lead: Pending
- QA Lead: Pending
- Date: Pending

