# Huong dan tich hop Authentication cho FrontEnd

Tai lieu nay duoc viet dua tren `MedMateAI/Controllers/AuthController.cs` va cac service/DTO lien quan. FE nen coi day la contract hien tai cua API authentication.

## 1. Thong tin chung

- Base path authentication: `/api/authentication`
- Protected API dung JWT Bearer trong header:

```http
Authorization: Bearer <accessToken>
```

- Backend dang cau hinh CORS cho origin `http://localhost:3000` va cho phep credentials.
- Cac request can nhan/gui refresh token cookie phai bat credentials:
  - `fetch`: `credentials: "include"`
  - `axios`: `withCredentials: true`
- Refresh token duoc backend set vao cookie ten `refreshToken` voi `HttpOnly`, `SameSite=Strict`, `Path=/`.
  - FE khong doc duoc cookie nay bang JavaScript.
  - FE chi can dam bao request login/google/refresh co credentials de browser tu luu va gui cookie.

## 2. Response format chung

Tat ca endpoint tra ve envelope:

```ts
type ApiResponse<T = undefined> = {
  success: boolean;
  message: string;
  errors: string[];
  data?: T;
};

type AuthResponse = {
  accessToken: string;
  email: string;
  userId: string;
  roles?: string[];
  expiresAtUtc: string;
};
```

Luu y:

- JSON mac dinh theo camelCase.
- `roles` hien co the khong duoc tra ve o mot so flow nhu Google login/refresh; neu FE can roles chac chan, goi them `GET /api/users/me` sau khi co access token.
- Khi `success = false`, FE nen uu tien hien thi `errors[]`; neu `errors` rong thi hien thi `message`.

## 3. Dang ky tai khoan

```http
POST /api/authentication/register
Content-Type: application/json
```

Request body:

```json
{
  "email": "user@example.com",
  "userName": "user@example.com",
  "password": "Password@123",
  "confirmPassword": "Password@123",
  "displayName": "Nguyen Van A",
  "address": "Ha Noi",
  "gender": 1,
  "dateOfBirth": "2000-01-31"
}
```

Field:

| Field | Bat buoc | Ghi chu |
| --- | --- | --- |
| `email` | Co | Email dang nhap, phai unique. |
| `userName` | Khuyen nghi | Backend se dung `email` neu `userName` rong. |
| `password` | Co | Theo rule Identity: toi thieu 8 ky tu, co chu thuong, chu hoa, so va ky tu dac biet. |
| `confirmPassword` | Co | Phai trung khop chinh xac voi `password`. |
| `displayName` | Co | Ten hien thi. |
| `address` | Khong | Chuoi dia chi. |
| `gender` | Khong | Enum dang number: `1 = Male`, `2 = Female`. |
| `dateOfBirth` | Khong | Dinh dang `YYYY-MM-DD`. |

Response:

- `200 OK`: dang ky thanh cong.
- `400 Bad Request`: loi validation/Identity.

Important: endpoint register hien tai khong tao access token su dung duoc va khong set refresh token cookie. Sau khi register thanh cong, FE nen dieu huong user sang man hinh login hoac tu dong goi login bang email/password vua nhap neu UX yeu cau.

## 4. Dang nhap bang email/password

```http
POST /api/authentication/login
Content-Type: application/json
```

Request body:

```json
{
  "email": "user@example.com",
  "password": "Password@123"
}
```

Response thanh cong:

```json
{
  "success": true,
  "message": "Login succeeded",
  "errors": [],
  "data": {
    "accessToken": "<jwt>",
    "email": "user@example.com",
    "userId": "00000000-0000-0000-0000-000000000000",
    "roles": ["User"],
    "expiresAtUtc": "2026-05-12T07:30:00+00:00"
  }
}
```

Backend dong thoi set `refreshToken` vao HttpOnly cookie. FE can:

1. Goi API voi `credentials: "include"` hoac `withCredentials: true`.
2. Luu `data.accessToken` vao auth state cua app.
3. Dung `Authorization: Bearer <accessToken>` cho API protected.
4. Dung `data.expiresAtUtc` de chu dong refresh truoc khi token het han.

Loi:

- `401 Unauthorized`: sai email/password hoac tai khoan dang bi lockout.
- Message backend: `"Invalid email or password."`

Sau 5 lan dang nhap sai, backend co cau hinh lockout 15 phut.

## 5. Dang nhap bang Google

```http
POST /api/authentication/google
Content-Type: application/json
```

Request body:

```json
{
  "credential": "<google-id-token>"
}
```

FE lay `credential` tu Google Identity Services, vi du `response.credential`, roi gui nguyen chuoi ID token len backend.

Thanh cong:

- `200 OK`
- Tra ve `AuthResponse`
- Set `refreshToken` HttpOnly cookie tuong tu login password.
- Neu email Google chua ton tai trong he thong, backend se tu tao user moi voi role `User`.

Loi:

- `400 Bad Request`: thieu credential, backend thieu Google ClientId, Google account khong tra email, hoac loi tao user.
- `401 Unauthorized`: Google credential khong hop le.

## 6. Refresh access token

```http
POST /api/authentication/refresh
```

Request:

- Khong can body.
- Bat buoc gui cookie:

```ts
await fetch(`${API_BASE_URL}/api/authentication/refresh`, {
  method: "POST",
  credentials: "include",
});
```

Response thanh cong:

- `200 OK`
- Tra ve access token moi trong `data.accessToken`.
- Backend hien tai khong rotate refresh token va khong set lai cookie moi.

Loi:

- `401 Unauthorized`
- Message: `"Refresh token missing, invalid, or expired."`

Khuyen nghi luong FE:

1. Khi app khoi dong, neu chua co access token trong memory, goi `/refresh` voi credentials de thu khoi phuc session.
2. Truoc khi access token het han, goi `/refresh`.
3. Neu protected API tra `401`, goi `/refresh` mot lan, cap nhat token roi retry request ban dau. Neu refresh van `401`, clear auth state va dua user ve login.

## 7. Quen mat khau: gui OTP

```http
POST /api/authentication/forgot-password
Content-Type: application/json
```

Request body:

```json
{
  "email": "user@example.com"
}
```

Response:

- `200 OK`: `"If the email exists, an OTP has been sent."`
- Backend co tinh tra ve cung message ngay ca khi email khong ton tai de tranh lo thong tin tai khoan.
- `400 Bad Request`: email rong hoac khong gui duoc OTP email.

OTP reset password hien tai het han sau khoang 1 phut.

## 8. Doi mat khau bang OTP

```http
POST /api/authentication/change-password
Content-Type: application/json
```

Request body:

```json
{
  "email": "user@example.com",
  "otp": "123456",
  "newPassword": "NewPassword@123",
  "confirmNewPassword": "NewPassword@123"
}
```

Response:

- `200 OK`: `"Password changed successfully."`
- `400 Bad Request`: email/OTP/password khong hop le, OTP sai/het han, password khong dat rule Identity.

FE validation nen ap dung:

- `email` khong rong.
- `otp` khong rong.
- `newPassword` toi thieu 8 ky tu, co chu thuong, chu hoa, so va ky tu dac biet.
- `confirmNewPassword` trung voi `newPassword`.

## 9. Goi API protected sau khi dang nhap

Vi du lay current user:

```http
GET /api/users/me
Authorization: Bearer <accessToken>
```

Response thanh cong:

```ts
type CurrentUserResponse = {
  userId: string;
  email: string;
  name: string;
  roles: string[];
};
```

## 10. Vi du API client cho FE

```ts
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

let accessToken: string | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

async function request<T>(path: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers);
  headers.set("Content-Type", "application/json");

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
    credentials: "include",
  });

  const payload = (await response.json()) as ApiResponse<T>;

  if (!response.ok || !payload.success) {
    throw payload;
  }

  return payload;
}

export async function login(email: string, password: string) {
  const res = await request<AuthResponse>("/api/authentication/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });

  setAccessToken(res.data?.accessToken ?? null);
  return res.data;
}

export async function refreshToken() {
  const res = await request<AuthResponse>("/api/authentication/refresh", {
    method: "POST",
  });

  setAccessToken(res.data?.accessToken ?? null);
  return res.data;
}
```

## 11. Checklist cho FrontEnd

- [ ] Cau hinh `VITE_API_BASE_URL`/base URL tro den backend.
- [ ] Tat ca auth calls dung `credentials: "include"` hoac `withCredentials: true`.
- [ ] Luu access token trong auth state va gan `Authorization: Bearer ...` cho protected APIs.
- [ ] Xu ly refresh token qua cookie, khong co gang doc cookie bang JS.
- [ ] Implement app bootstrap: goi `/api/authentication/refresh` de restore session neu can.
- [ ] Implement retry mot lan khi protected API bi `401`.
- [ ] Sau register thanh cong, dua user sang login hoac goi login rieng.
- [ ] Man hinh forgot/change password thong bao OTP het han nhanh, hien tai khoang 1 phut.
- [ ] Logout hien chua co endpoint backend; FE chi co the clear access token local. Neu can logout that su, can bo sung endpoint backend de revoke refresh token va clear cookie.
- [ ] Neu deploy FE khac origin/domain, can dong bo lai CORS va cookie policy voi backend.
